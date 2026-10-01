using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Aoe4Helper.Production
{
   // 字段名与 EngineObjectModel.dll 的 DataContract 保持一致。
   [DataContract]
   public sealed class RpcEntity
   {
      [DataMember] public uint EntityId { get; set; }
      [DataMember] public uint OwnerId { get; set; }
      [DataMember] public string BlueprintName { get; set; }
      [DataMember] public RpcProductionQueue ProductionQueue { get; set; }
   }

   [DataContract]
   public sealed class RpcProductionQueue
   {
      [DataMember] public List<RpcQueuedItem> QueuedItems { get; set; }
      [DataMember] public List<RpcProduceableItem> ProduceableItems { get; set; }
   }

   [DataContract]
   public sealed class RpcQueuedItem
   {
      [DataMember] public uint ItemId { get; set; }
      [DataMember] public byte ItemType { get; set; }
      [DataMember] public float Progress { get; set; }
   }

   [DataContract]
   public sealed class RpcProduceableItem
   {
      // ProductionItemType.Spawn 的底层值为 byte 1。
      public const byte Spawn = 1;
      [DataMember] public uint Id { get; set; }
      [DataMember] public byte Type { get; set; }
      [DataMember] public string Name { get; set; }
      [DataMember] public bool CanBeProduced { get; set; }
   }
}
