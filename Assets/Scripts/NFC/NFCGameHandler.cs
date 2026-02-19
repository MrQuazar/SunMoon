using UnityEngine;
using DigitsNFCToolkit.Samples;

namespace DigitsNFCToolkit.Samples
{
    public class NFCGameHandler : MonoBehaviour
    {
        private NDEFMessage pendingMessage;

        #region Read
        public void OnMakeReadonlyClick()
        {
#if (!UNITY_EDITOR)
			NativeNFCManager.Enable();
			NativeNFCManager.RequestNDEFMakeReadonly();
#endif
        }

        public void OnNDEFReadFinished(NDEFReadResult result)
        {
            string readResultString = string.Empty;
            if (result.Success)
            {
                readResultString = string.Format("NDEF Message was read successfully from tag {0}", result.TagID);
                // view.UpdateNDEFMessage(result.Message);
            }
            else
            {
                readResultString = string.Format("Failed to read NDEF Message from tag {0}\nError: {1}", result.TagID, result.Error);
            }
            Debug.Log(readResultString);
        }

        #endregion

        #region Write

        public void OnNDEFPushFinished(NDEFPushResult result)
        {
            string pushResultString = string.Empty;
            if (result.Success)
            {
                pushResultString = "NDEF Message pushed successfully to other device";
            }
            else
            {
                pushResultString = "NDEF Message failed to push to other device";
            }
            Debug.Log(pushResultString);
        }

        public void OnPushMessageClick()
        {
            if (pendingMessage != null)
            {
#if (!UNITY_EDITOR) && UNITY_ANDROID
				NativeNFCManager.RequestNDEFPush(pendingMessage);
#endif
            }
        }

        #endregion Write
    }
}