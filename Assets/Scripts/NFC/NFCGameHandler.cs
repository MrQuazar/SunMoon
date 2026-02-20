using UnityEngine;
using DigitsNFCToolkit.Samples;

namespace DigitsNFCToolkit.Samples
{
    public class NFCGameHandler : MonoBehaviour
    {
        public static NFCGameHandler instance;
        private NDEFMessage pendingMessage;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Read
        public void OnMakeReadonlyClick()
        {
#if (!UNITY_EDITOR)
// Register listener to detect NFC tag info
NativeNFCManager.AddNFCTagDetectedListener(OnNFCTagDetected);

// Register listener to handle NDEF message read result
NativeNFCManager.AddNDEFReadFinishedListener(OnNDEFReadFinished);
			NativeNFCManager.Enable();
			NativeNFCManager.RequestNDEFMakeReadonly();
#endif
        }

        void OnNFCTagDetected(NFCTag tag)
        {
            // Access tag information
            Debug.Log("Tag ID: " + tag.ID);
            Debug.Log("Technologies: " + string.Join(",", tag.Technologies));
            Debug.Log("Writable: " + tag.Writable);
        }

        public void OnNDEFReadFinished(NDEFReadResult result)
        {
            string readResultString = string.Empty;
            //Get The TextRecord
            result.Message.Records.ForEach(record =>
            {
                if (record is TextRecord)
                {
                    TextRecord textRecord = record as TextRecord;
                    readResultString += string.Format("Text Record: {0}\n", textRecord.text);
                    Debug.LogError(readResultString);
                }
            });
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

        public void OnPushMessageClick(string message)
        {
            pendingMessage = new NDEFMessage();
            pendingMessage.Records.Add(new TextRecord(message));
            if (pendingMessage != null)
            {
                Debug.LogError(pendingMessage);

#if (!UNITY_EDITOR) && UNITY_ANDROID
				NativeNFCManager.RequestNDEFPush(pendingMessage);
#endif
            }
        }

        #endregion Write
    }
}