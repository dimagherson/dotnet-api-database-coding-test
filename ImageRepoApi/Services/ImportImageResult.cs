namespace ImageRepoApi.Services
{
    public struct ImportImageResult
    {
        internal ImportImageResult(Guid imageId, bool alreadyExists)
        {
            ImageId = imageId;
            AlreadyExists = alreadyExists;
        }

        public Guid ImageId { get; }
        public bool AlreadyExists { get; }
    }
}
