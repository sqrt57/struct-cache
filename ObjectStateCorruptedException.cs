using System;

namespace Papirus.Cache.LongLived
{
	public class ObjectStateCorruptedException : ApplicationException
	{
		public ObjectStateCorruptedException() : base() { }
		public ObjectStateCorruptedException(string message) : base(message) { }
		public ObjectStateCorruptedException(string message, Exception innerException) : base(message, innerException) { }
	}
}
