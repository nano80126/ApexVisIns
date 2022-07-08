using System;
using System.Runtime.Serialization;

/*
    新增 Camera Exception
    新增 Motion Exception
    新增 IOCard Exception
    新增 Light  Exception
*/

namespace MCAJawIns
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

    /// <summary>
    /// 相機相關例外
    /// </summary>
    [Serializable]
    public class CameraException : Exception
    {
        public CameraException() { }
        public CameraException(string message) : base(message) { }
        public CameraException(string message, Exception inner) : base(message, inner) { }
        protected CameraException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// WISE-4050 IO 控制相關例外
    /// </summary>
    [Serializable]
    public class WISE4050Exception : Exception
    {
        public WISE4050Exception() { }
        public WISE4050Exception(string message) : base(message) { }
        public WISE4050Exception(string message, Exception inner) : base(message, inner) { }
        protected WISE4050Exception(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// 光源控制相關例外
    /// </summary>
    [Serializable]
    public class LightCtrlException : Exception
    {
        public LightCtrlException() { }
        public LightCtrlException(string message) : base(message) { }
        public LightCtrlException(string message, Exception inner) : base(message, inner) { }
        protected LightCtrlException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class DatabaseException : Exception
    {
        public DatabaseException() { }
        public DatabaseException(string message) : base(message) { }
        public DatabaseException(string message, Exception inner) : base(message, inner) { }
        protected DatabaseException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// MCA JAW 檢驗相關例外
    /// </summary>
    [Serializable]
    public class MCAJawException : Exception
    {
        public MCAJawException() { }
        public MCAJawException(string message) : base(message) { }
        public MCAJawException(string message, Exception inner) : base(message, inner) { }
        protected MCAJawException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
