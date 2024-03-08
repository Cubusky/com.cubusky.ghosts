using System;
using Unity.Serialization.Json;

namespace Cubusky.Ghosts
{
    public record FloatRounder : IJsonAdapter<float>
    {
        public readonly int digits;
        public readonly MidpointRounding mode;

        public FloatRounder(int digits) : this(digits, default) { }
        public FloatRounder(int digits, MidpointRounding mode)
        {
            this.digits = digits;
            this.mode = mode;
        }

        void IJsonAdapter<float>.Serialize(in JsonSerializationContext<float> context, float value) => context.Writer.WriteValue(MathF.Round(value, digits, mode));
        float IJsonAdapter<float>.Deserialize(in JsonDeserializationContext<float> context) => MathF.Round(context.ContinueVisitation(), digits, mode);
    }
}
