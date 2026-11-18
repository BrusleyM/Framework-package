namespace Framework.Tasks
{
    public static class SubtaskStateRules
    {
        public static bool ResolvesToOn(SubtaskState state, bool isCurrentlyOn)
        {
            switch (state)
            {
                case SubtaskState.On:
                    return true;
                case SubtaskState.Off:
                    return false;
                default:
                    return !isCurrentlyOn;
            }
        }
    }
}
