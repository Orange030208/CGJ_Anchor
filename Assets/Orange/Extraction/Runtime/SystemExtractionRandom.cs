using System;

namespace Orange.Extraction
{
    public sealed class SystemExtractionRandom : IExtractionRandom
    {
        private readonly Random random;

        public SystemExtractionRandom()
            : this(new Random())
        {
        }

        public SystemExtractionRandom(int seed)
            : this(new Random(seed))
        {
        }

        public SystemExtractionRandom(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public float NextNormalizedValue()
        {
            float value = (float)random.NextDouble();
            // Rounding to float can turn a value below 1 into 1.
            return value == 1f ? 0.99999994f : value;
        }
    }
}
