//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Rs007Control
{
    [Serializable]
    public class Rs007Joints6Msg : Message
    {
        public const string k_RosMessageName = "rs007_control/Rs007Joints6";
        public override string RosMessageName => k_RosMessageName;

        public double[] joints;

        public Rs007Joints6Msg()
        {
            this.joints = new double[6];
        }

        public Rs007Joints6Msg(double[] joints)
        {
            this.joints = joints;
        }

        public static Rs007Joints6Msg Deserialize(MessageDeserializer deserializer) => new Rs007Joints6Msg(deserializer);

        private Rs007Joints6Msg(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.joints, sizeof(double), 6);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.joints);
        }

        public override string ToString()
        {
            return "Rs007Joints6Msg: " +
            "\njoints: " + System.String.Join(", ", joints.ToList());
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}