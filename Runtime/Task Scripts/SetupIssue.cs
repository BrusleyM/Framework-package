namespace Framework.Tasks
{
    public readonly struct SetupIssue
    {
        public SetupIssue(string message)
        {
            Message = message;
        }

        public string Message { get; }

        public override string ToString()
        {
            return Message;
        }
    }
}
