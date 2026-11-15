namespace Framework.Sequencing
{
    public static class Sequence
    {
        public static SequenceBuilder Named(string sequenceName)
        {
            return new SequenceBuilder(sequenceName);
        }
    }
}
