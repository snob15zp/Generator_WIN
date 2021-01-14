namespace GeneratorServerApi.Model
{
    class ResponseError
    {
        public Error errors { get; set; }
    }

    class Error
    {
        public int status { get; set; }
        public string message { get; set; }
    }
}
