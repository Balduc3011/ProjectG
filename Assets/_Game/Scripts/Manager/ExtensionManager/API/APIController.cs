using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class APIController : MonoBehaviour
{
    public int timeoutSeconds = 10;
    public CancellationToken externalToken = default;

    public async UniTask CallAPIGetAsync<T>(
        string apiEndpoint,
        Action<ApiSuccessResponse<T>> onSuccess, 
        Action<ApiErrorResponse> onError
    )
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        using UnityWebRequest request = UnityWebRequest.Get(apiEndpoint);
        request.timeout = timeoutSeconds;

        try
        {
            await request.SendWebRequest().WithCancellation(cts.Token);

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"API Error: {request.error}");
            }
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"GET timeout after {timeoutSeconds}s: {apiEndpoint}");
        }
    }
    }
