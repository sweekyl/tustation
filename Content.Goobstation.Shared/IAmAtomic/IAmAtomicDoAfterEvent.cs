using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.IAmAtomic;

[Serializable, NetSerializable]
public sealed partial class IAmAtomicDoAfterEvent : SimpleDoAfterEvent;
