using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace NavKeypad
{
    public class Keypad : MonoBehaviour
    {
        [Header("Events")]
        [SerializeField] private UnityEvent onAccessGranted;
        [SerializeField] private UnityEvent onAccessDenied;
        [Header("Combination Code (9 Numbers Max)")]
        [SerializeField] private int keypadCombo = 12345;

        public UnityEvent OnAccessGranted => onAccessGranted;
        public UnityEvent OnAccessDenied => onAccessDenied;

        [Header("Settings")]
        [SerializeField] private string accessGrantedText = "Granted";
        [SerializeField] private string accessDeniedText = "Denied";

        [Header("Visuals")]
        [SerializeField] private float displayResultTime = 1f;
        [Range(0, 5)]
        [SerializeField] private float screenIntensity = 2.5f;
        [Header("Colors")]
        [SerializeField] private Color screenNormalColor = new Color(0.98f, 0.50f, 0.032f, 1f); //orangy
        [SerializeField] private Color screenDeniedColor = new Color(1f, 0f, 0f, 1f); //red
        [SerializeField] private Color screenGrantedColor = new Color(0f, 0.62f, 0.07f); //greenish
        [Header("SoundFx")]
        [SerializeField] private AudioClip buttonClickedSfx;
        [SerializeField] private AudioClip accessDeniedSfx;
        [SerializeField] private AudioClip accessGrantedSfx;
        [Header("Component References")]
        [SerializeField] private Renderer panelMesh;
        [SerializeField] private TMP_Text keypadDisplayText;
        [SerializeField] private AudioSource audioSource;

        [SerializeField] private AudioClip doorMovingSfx;  // 문 올라가는 중 반복 재생할 소리
        [SerializeField] private AudioClip doorOpenedSfx;  // 문 완전히 올라갔을 때 재생할 소리
        [SerializeField] private AudioClip chestOpenSfx;

        private AudioSource doorAudioSource;


        private string currentInput;
        private bool displayingResult = false;
        private bool accessWasGranted = false;

        public GameObject LiftObject;

        private void Awake()
        {
            ClearInput();
            panelMesh.material.SetVector("_EmissionColor", screenNormalColor * screenIntensity);
            doorAudioSource = gameObject.AddComponent<AudioSource>();
            doorAudioSource.loop = true;
            doorAudioSource.playOnAwake = false;
        }


        //Gets value from pressedbutton
        public void AddInput(string input)
        {
            audioSource.PlayOneShot(buttonClickedSfx);
            if (displayingResult || accessWasGranted) return;
            switch (input)
            {
                case "enter":
                    CheckCombo();
                    break;
                default:
                    if (currentInput != null && currentInput.Length == 9) // 9 max passcode size 
                    {
                        return;
                    }
                    currentInput += input;
                    keypadDisplayText.text = currentInput;
                    break;
            }

        }
        public void CheckCombo()
        {
            if (int.TryParse(currentInput, out var currentKombo))
            {
                bool granted = currentKombo == keypadCombo;
                if (!displayingResult)
                {
                    StartCoroutine(DisplayResultRoutine(granted));
                }
            }
            else
            {
                Debug.LogWarning("Couldn't process input for some reason..");
            }

        }

        //mainly for animations 
        private IEnumerator DisplayResultRoutine(bool granted)
        {
            displayingResult = true;

            if (granted) AccessGranted();
            else AccessDenied();

            yield return new WaitForSeconds(displayResultTime);
            displayingResult = false;
            if (granted) yield break;
            ClearInput();
            panelMesh.material.SetVector("_EmissionColor", screenNormalColor * screenIntensity);

        }

        private void AccessDenied()
        {
            keypadDisplayText.text = accessDeniedText;
            onAccessDenied?.Invoke();
            panelMesh.material.SetVector("_EmissionColor", screenDeniedColor * screenIntensity);
            audioSource.PlayOneShot(accessDeniedSfx);
        }

        private void ClearInput()
        {
            currentInput = "";
            keypadDisplayText.text = currentInput;
        }

        private void AccessGranted()
        {
            accessWasGranted = true;
            keypadDisplayText.text = accessGrantedText;
            onAccessGranted?.Invoke();
            panelMesh.material.SetVector("_EmissionColor", screenGrantedColor * screenIntensity);
            audioSource.PlayOneShot(accessGrantedSfx);

            if (LiftObject != null && LiftObject.CompareTag("LiftObject"))
            {
                StartCoroutine(MoveDoorUpRoutine(LiftObject.transform, 4f, 3f));
            }
            if (LiftObject != null && LiftObject.CompareTag("chest"))
            {
                LiftObject.transform.rotation = Quaternion.Euler(0, -110f, 0);
                audioSource.PlayOneShot(chestOpenSfx);
            }
            if (LiftObject != null && LiftObject.CompareTag("Door"))
            {
                StartCoroutine(MoveDoorUpRoutine(LiftObject.transform, 4.5f, 3f));
            }
        }
        private IEnumerator MoveDoorUpRoutine(Transform target, float distance, float duration)
        {
            doorAudioSource.clip = doorMovingSfx;
            doorAudioSource.loop = true;
            doorAudioSource.Play();

            Vector3 startPos = target.position;
            Vector3 endPos = startPos + new Vector3(0, distance, 0);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                target.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            target.position = endPos;

            // 문 올라가는 소리 멈추고,
            doorAudioSource.Stop();

            // 문 완전히 올라갔을 때 재생할 소리 재생 (한번만)
            doorAudioSource.loop = false;
            doorAudioSource.clip = doorOpenedSfx;
            doorAudioSource.Play();
        }

    }
}