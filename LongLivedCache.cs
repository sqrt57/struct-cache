using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Papirus.Cache.LongLived
{
	public class LongLivedStructCache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
		where TKey: struct
		where TValue: struct
	{
		private readonly IEqualityComparer<TKey> _comparer;
		private Node[] _nodes;
		private readonly Dictionary<TKey, int> _indices;
		private int _nextFreeIndex;
		private int _freeList;

		public LongLivedStructCache() : this(null) {}

		public LongLivedStructCache(IEqualityComparer<TKey> comparer)
		{
			_comparer = comparer ?? EqualityComparer<TKey>.Default;
			_indices = new Dictionary<TKey, int>(_comparer);
			_nodes = new Node[16];
			_nextFreeIndex = 0;
			_freeList = -1;
		}

		public void AddOrUpdate(TKey key, TValue value)
		{
			if (!_indices.TryGetValue(key, out int index))
			{
				index = GetFreeIndex();
				_indices[key] = index;
				_nodes[index].state = NodeState.Occupied;
				_nodes[index].key = key;
			}

			_nodes[index].value = value;
		}

		public bool TryGet(TKey key, out TValue result)
		{
			if (_indices.TryGetValue(key, out int index))
			{
				result = _nodes[index].value;
				return true;
			}
			else
			{
				result = default;
				return false;
			}
		}

		private int GetFreeIndex()
		{
			if (_freeList >= 0)
			{
				var result = _freeList;
				_freeList = _nodes[_freeList].next;
				return result;
			}

			if (_nextFreeIndex >= _nodes.Length)
			{
				var newNodes = new Node[2 * _nodes.Length];
				_nodes.CopyTo(newNodes, 0);
				_nodes = newNodes;
			}

			return _nextFreeIndex++;
		}

		public void Remove(TKey key)
		{
			if (_indices.TryGetValue(key, out int index))
			{
				_indices.Remove(key);
				_nodes[index].state = NodeState.Free;
				_nodes[index].next = _freeList;
				_freeList = index;
			}
		}

		public int Capacity => _nodes.Length;

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			for (int i = 0; i < _nodes.Length; i++)
			{
				if (_nodes[i].state == NodeState.Occupied)
					yield return new KeyValuePair<TKey, TValue>(_nodes[i].key, _nodes[i].value);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void CheckStateAndThrowIfCorrupted()
		{
			if (_nextFreeIndex < 0)
				throw new ObjectStateCorruptedException($"Next free index {_nextFreeIndex} is negative");

			if (_nextFreeIndex > _nodes.Length)
				throw new ObjectStateCorruptedException($"Next free index {_nextFreeIndex} should be less or equal to nodes array length {_nodes.Length}");

			foreach (var pair in _indices)
			{
				if (pair.Value < 0 || pair.Value > _nodes.Length)
					throw new ObjectStateCorruptedException($"Key {pair.Key} points to index {pair.Value} outside of nodes array bounds");

				if (!_comparer.Equals(_nodes[pair.Value].key, pair.Key))
					throw new ObjectStateCorruptedException($"Key {pair.Key} points to node {pair.Value} with another key value: {_nodes[pair.Value].key}");

				if (_nodes[pair.Value].state != NodeState.Occupied)
					throw new ObjectStateCorruptedException($"Key {pair.Key} points to node {pair.Value} with state: {_nodes[pair.Value].state}");
			}

			for (int i = 0; i < _nextFreeIndex; i++)
			{
				switch (_nodes[i].state)
				{
					case NodeState.Free:
						break;
					case NodeState.Occupied:
						if (!_indices.ContainsKey(_nodes[i].key))
							throw new ObjectStateCorruptedException($"Node {i} is occupied but its key {_nodes[i].key} is not in indices map");

						if (!Equals(_indices[_nodes[i].key], i))
							throw new ObjectStateCorruptedException($"Node {i} is occupied but its key {_nodes[i].key} is mapped to another index {_indices[_nodes[i].key]}");

						break;
					default:
						throw new ObjectStateCorruptedException($"Node {i} has unknown state {_nodes[i].state}");
				}
			}

			for (int i = _nextFreeIndex; i < _nodes.Length; i++)
			{
				if (_nodes[i].state != NodeState.Free)
					throw new ObjectStateCorruptedException($"Node {i} is above next free index, but it's state is {_nodes[i].state}");
			}

			int cur = _freeList;
			for (int i = 0; i < _nodes.Length; i++)
			{
				if (cur < 0)
					break;

				if (cur >= _nextFreeIndex)
					throw new ObjectStateCorruptedException($"Free list next link {cur} points after next free index {_nextFreeIndex}");

				if (_nodes[cur].state != NodeState.Free)
					throw new ObjectStateCorruptedException($"Node {cur} is in free list, but it's state is {_nodes[cur].state}");

				cur = _nodes[cur].next;
			}

			if (cur != -1)
				throw new ObjectStateCorruptedException($"Cycle in free node list including node {cur}");
		}

		private struct Node
		{
			public int next;
			public NodeState state;
			public TKey key;
			public TValue value;
		}

		private enum NodeState
		{
			Free = 0,
			Occupied
		}
	}

	public class LongLivedCache<Tkey, TSavedValue, TValue>
		where Tkey : struct
		where TSavedValue : struct
		where TValue : class
	{
		private readonly Func<TValue, TSavedValue> _save;
		private readonly Func<TSavedValue, TValue> _load;

		public LongLivedCache(Func<TValue, TSavedValue> save, Func<TSavedValue, TValue> load)
		{
			_save = save ?? throw new ArgumentNullException(nameof(save));
			_load = load ?? throw new ArgumentNullException(nameof(load));
		}
	}
}
