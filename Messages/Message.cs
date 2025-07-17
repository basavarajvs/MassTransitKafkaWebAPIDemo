namespace Messages
{
    public record Message
    {
        public Guid Id { get; init; }
        public required string Text { get; init; }
    }
}