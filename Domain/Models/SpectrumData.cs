namespace Domain.Models
{
    public class SpectrumData
    {
        public double[] PowerLevels { get; }
        public DateTime Timestamp { get; }

        public SpectrumData(double[] powerLevels)
        {
            PowerLevels = powerLevels ?? throw new ArgumentNullException(nameof(powerLevels));
            Timestamp = DateTime.Now;
        }

        public int Length => PowerLevels.Length;

        public double this[int index] => PowerLevels[index];
    }
}
