namespace test3.Models
{
    public class ErrorData
    {
        public ErrorData()
        {
        }

        public ErrorData(ErrorType errorType, string data)
        {
            ErrorType = errorType;
            Data = data;
        }

        public string Data { get; set; }
        public ErrorType ErrorType { get; set; }
    }
}

