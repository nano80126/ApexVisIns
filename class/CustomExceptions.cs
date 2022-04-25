using System;
using System.Runtime.Serialization;


/*
    新增 Camera Exception
    新增 Motion Exception
    新增 IOCard Exception
    新增 Light  Exception
*/


namespace ApexVisIns
{
    /// <summary>
    /// 拋出例外表示這區塊不應該被執行到
    /// </summary>
    [Serializable]
    public class ShouldNotBeReachedException : Exception
    {
        public ShouldNotBeReachedException() { }
        public ShouldNotBeReachedException(string message) : base(message) { }
        public ShouldNotBeReachedException(string message, Exception inner) : base(message, inner) { }
        protected ShouldNotBeReachedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// EtherCAT軸卡/馬達相關例外
    /// </summary>
    [Serializable]
    public class MotorException : Exception
    {
        public MotorException() { }
        public MotorException(string message) : base(message) { }
        public MotorException(string message, Exception inner) : base(message, inner) { }
        protected MotorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
