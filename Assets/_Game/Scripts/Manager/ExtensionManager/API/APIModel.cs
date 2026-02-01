public class APIModel
{
    public static string apiEndpoint_Time;
}

[System.Serializable]
public class ApiSuccessResponse<T>
{
    public bool success;
    public string message;
    public T data;
    public int statusCode;
    public string responseDate;
}
    
[System.Serializable]
public class ApiErrorResponse
{
    public bool success;
    public string message;
    public string error;
    public int statusCode;
    public string responseDate;
}
