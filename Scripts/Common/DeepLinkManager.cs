using System;
using UnityEngine;

public class DeepLinkManager : MonoBehaviour
{
    public static DeepLinkManager Instance { get; private set; }
    public string deeplinkURL;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Application.deepLinkActivated += onDeepLinkActivated;
            if (!String.IsNullOrEmpty(Application.absoluteURL))
            {
                onDeepLinkActivated(Application.absoluteURL);
            }
            else deeplinkURL = "[none]";

            Logger.Log($"HGKIM || DeepLinkURL : {deeplinkURL}");
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Logger.Log($"HGKIM || DeepLinkURL : instance null");
            Destroy(gameObject);
        }

        // 230428 - 메일 스킴 테스트
        //GameData.isMailPush = true;
    }

    // HGKIM 221229 - iOS 웹뷰 스킴 캐치를 위해 생성
    Action<string> iOS_action; 

    public void SetSchemeAction(Action<string> action)
	{
        iOS_action -= action;   // 중복 적용 방지용
        iOS_action += action;
    }

    public void RemoveSchmeAction(Action<string> action)
	{
        iOS_action -= action;
	}
    // HGKIM 221229 - iOS 웹뷰 스킴 캐치를 위해 생성 여기까지

    private void onDeepLinkActivated(string url)
    {
        Logger.Log("Invoked from the url: " + url);

        deeplinkURL = url;
        Uri deepLinkURI = new Uri(deeplinkURL);

        string queryString = deepLinkURI.Query;

        Logger.Log($"HGKIM || DeepLinkURL Catch : {deepLinkURI.ToString()}");

        // HGKIM 221229 - iOS 에서 스킴 URL이 이쪽으로 와서 여기서 캐치를 한다.
        if(url.Contains("pengtalk://sso-login"))    // 로그인에 대한 캐치
		{
            iOS_action.Invoke(url);
            return;
		}
        else if(url.Contains("pengtalk://sso-user"))    // 정보 변경에 대한 캐치
        {
            iOS_action.Invoke(url);
            return;
        }
        else if(url.Contains("pengtalk://sso-secession"))   // 회원 탈퇴에 관한 캐치
        {
            iOS_action.Invoke(url);
            return;
        }
        // 나머지는 아래에서 처리한다. (푸시 등)

        var queryParams = System.Web.HttpUtility.ParseQueryString(queryString);

		// SAMPLES (외부 호출)
		// 샘플 콜: pengtalk://open?command=true

		if (queryParams != null)
		{
            Logger.Log($"HGKIM || DeepLinkURL queryParams is not null");
			string command = queryParams["command"];

			// 230317 - 푸시 처리부 변경
			//if (command.Equals("true"))
			//{
			//	GameData.isTodayWordPush = true;
			//             Logger.Log($"HGKIM || DeepLinkURL queryParams is true");

			//         }
			//else
			//{
			//	GameData.isTodayWordPush = false;
			//             Logger.Log($"HGKIM || DeepLinkURL queryParams is false");

			//         }
			switch (command)
			{
                //pengtalk://open?command=true
                case "true":    // 오늘의 영단어 스킴
                    GameData.isTodayWordPush = true;
                    Logger.Log($"HGKIM || DeepLinkURL queryParams is true");
                    break;

                //pengtalk://open?command=mail
                case "mail":    // 230317 - 메일 스킴 <- command 에 담겨오는 text가 바뀌면 여기를 바꾼다.
                    GameData.isMailPush = true;
                    Logger.Log("DeepLinkURL queryParams is mail");
                    break;

                default:
                    Logger.Log($"Fail DeepLinkURL String Data!!!! ==> {command}");
                    break;
            }
		}
	}
}
