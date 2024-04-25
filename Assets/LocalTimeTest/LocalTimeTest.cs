using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Threading;
using Cysharp.Threading;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine.Networking;

public class LocalTimeTest : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI endTimeText;



    private CancellationTokenSource cts = new CancellationTokenSource();
    
    private double initServerTime;
    private float initClientStartupTime;
    private float timeOffset;
    private int timezoneHour;


    private void OnEnable()
    {
        if (cts != null)
        {
            cts.Dispose();
        }
        cts = new CancellationTokenSource();
    }

    private void OnDisable()
    {
        cts.Cancel();
    }

    private void Start()
    {
        PlayCountDown().Forget();
    }

    private double ConvertDatetimeToUnixTime(DateTime _dateTime)
    {
        return (_dateTime - DateTime.UnixEpoch).TotalSeconds;
    }
    private long ConvertDatetimeToUnixTime2(DateTime _dateTime)
    {
        return ((DateTimeOffset)_dateTime).ToUnixTimeSeconds();
    }

    private DateTime ConvertUnixTimeToDateTime(long _unixtime)
    {
        DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dt = dt.AddSeconds(_unixtime);
        return dt;
    }

    public long GetNextLocalMidnightTime()
    {
        var currDate = ConvertUnixTimeToDateTime(GetCurrUnixTime());
        //currDate = currDate.AddSeconds(timeOffset);
        //currDate = currDate.AddHours(timezoneHour);
        DateTime dt = new DateTime(currDate.Year, currDate.Month, currDate.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1).AddSeconds(-timeOffset);
        return ConvertDatetimeToUnixTime2(dt);
    }
    
    public long GetCurrUnixTime()
    {
        return (long)(initServerTime + (Time.realtimeSinceStartup - initClientStartupTime));
    }

    private string ConvertTimeLeftString3Cdhipers(long _seconds)
    {
        TimeSpan timeLeft = TimeSpan.FromSeconds(_seconds);
        return String.Format("{0:00}:{1:00}:{2:00}", timeLeft.Hours, timeLeft.Minutes, timeLeft.Seconds);
    }

    private async UniTask RequestServerTime()
    {
        try
        {
            var webrequest = await UnityWebRequest.Get("http://naver.com").SendWebRequest().WithCancellation(cts.Token);
            string date = webrequest.GetResponseHeader("date");
            Debug.Log($"time {date}");
            DateTime utcTime = DateTime.Parse(date).ToUniversalTime();
            DateTime localTime = DateTime.Parse(date).ToLocalTime();

            float tempTimeOffset = (float)(localTime - utcTime).TotalSeconds;
            timeOffset = PlayerPrefs.GetFloat("TimeOffset", 999.0f);
            if (timeOffset == 999.0f)
            {
                timeOffset = tempTimeOffset;
                PlayerPrefs.SetFloat("TimeOffset", timeOffset);
            }
            //timezoneHour = (int)DateTimeOffset.Now.Offset.TotalHours;

            initServerTime = ConvertDatetimeToUnixTime(utcTime);
            initClientStartupTime = Time.realtimeSinceStartup;

            Debug.Log($"LocalTime {localTime}");
        }
        catch
        {
            Debug.LogError("RequestServerTime Error");
        }
    }
    private async UniTaskVoid PlayCountDown()
    {
        await RequestServerTime();

        long endTime = GetNextLocalMidnightTime();

        PlayerLoopTimer.StartNew(TimeSpan.FromSeconds(1f), true, DelayType.DeltaTime, PlayerLoopTiming.Update, cts.Token, _ => {
            
            long timeLeft = endTime - GetCurrUnixTime();
            endTimeText.SetText(ConvertTimeLeftString3Cdhipers(timeLeft));
        }, null);
    }
}
