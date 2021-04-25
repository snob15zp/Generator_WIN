namespace GeneratorServerApi.Model
{
    class ResponseError
    {
        public Error Errors { get; set; }
    }

    class Error
    {
        public int Status { get; set; }
        public string Message { get; set; }
    }
}
