using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Linq;
using UnityEngine.Events;

namespace VirtualKeyboardUI
{
    public class VirtualKeyboard : MonoBehaviour
    { 
        #region Variables

        public static VirtualKeyboard instance;
        private EventSystem eventSystem;
        private InputField selectedInputField;
        private GameObject selectedObject;
        private GameObject previousSelectedObject = null;
        public Vector2 visiblePosition;
        public Vector2 hiddenPosition;
        private Vector2 oldMousePosition;
        public float speed;
        [SerializeField] private Button[] alphabet;
        private bool capsLock = true;
        private bool numOnlyActivated = false;
        private bool isDraggable = false;
        private bool active = false;
        public bool ActiveInScene 
        {
            get => active;
            private set => active = value;
        }
        public IEnumerable<GameObject> SceneInputFields
        {
            get => GetAllInputFieldsInScene();
        }

        #endregion

        void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Destroy(this.gameObject);
        }

        private IEnumerable<GameObject> GetAllInputFieldsInScene()
        {
            var inputs = FindObjectsOfType(typeof(InputField)) as InputField[];
            return inputs.Select(x => x.gameObject).ToList();
        }

        public void InsertChar()
        {
            var charToInsert = selectedObject.GetComponentInChildren<Text>().text;

            bool logic = selectedInputField.characterLimit == 0;
            logic |= selectedInputField.text.Length < selectedInputField.characterLimit;

            if (logic) selectedInputField.text += charToInsert;
            StartCoroutine(SetCaretToEnd());
        }

        public void InsertSpecialChar()
        {
            string newText = selectedInputField.text;

            switch (eventSystem.currentSelectedGameObject.name)
            {
                case "Spacebar":
                    newText += " ";
                    break;
                case "Backspace":
                    if (!String.IsNullOrEmpty(newText))
                        newText = newText.Remove(newText.Length - 1);
                    break;
                case "Enter":
                    if (selectedInputField.lineType == InputField.LineType.MultiLineNewline)
                        newText += "\n";
                    else newText += "\r";
                    break;                        
                default: break;
            }

            selectedInputField.text = newText;
            StartCoroutine(SetCaretToEnd());
        }

        private bool IsNumberInput(ref InputField input)
        {
            bool result = input.gameObject.name.ToLower().Contains("phone");
            if (!result)
                result |= input.contentType == InputField.ContentType.IntegerNumber;

            return result;
        }

        public void MoveByDrag()
        {
            if (isDraggable)
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 deltaPosition = mousePosition - oldMousePosition;
                transform.position = new Vector3(transform.position.x + deltaPosition.x,
                                                transform.position.y + deltaPosition.y);
            }
        }

        private void SelectInputField()
        {
            selectedInputField = selectedObject.GetComponent<InputField>();
            bool isNumberInput = IsNumberInput(ref selectedInputField);

            if (isNumberInput && !numOnlyActivated)
                SetNumberLock(true);
            else if (!isNumberInput && numOnlyActivated)
                SetNumberLock(false);

            numOnlyActivated = isNumberInput;
        }

        public void SelectObject()
        {
            bool logic = previousSelectedObject == null;
            logic |= eventSystem.currentSelectedGameObject.GetComponent<InputField>();

            if (logic)
                previousSelectedObject = selectedObject;

            selectedObject = eventSystem.currentSelectedGameObject;
        }

        private IEnumerator SetCaretToEnd()
        {
            eventSystem.SetSelectedGameObject(selectedInputField.gameObject);
            yield return new WaitForEndOfFrame();
            selectedInputField.MoveTextEnd(false);
        }

        public void SetInputFieldListeners()
        {
            foreach (var input in SceneInputFields)
            {
                if (input.GetComponent<EventTrigger>() == null)
                    input.AddComponent<EventTrigger>();
                EventTrigger eventTrigger = input.GetComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                entry.callback.AddListener((eventData) =>
                {
                    ToggleVirtualKeyboard(true);
                    SelectObject();
                    SelectInputField();
                });
                eventTrigger.triggers.Add(entry);
            }
        }

        public void SetNumberLock(bool value)
        {
            foreach (var button in alphabet)
            {
                button.interactable = !value;
            }
        }

        public IEnumerator SmoothMovement(Vector3 finalPosition)
        {
            float sqrRemainingDistance = (transform.position - finalPosition).sqrMagnitude;

            while (sqrRemainingDistance > 0.001f)
            {
                Vector2 newPosition = Vector2.MoveTowards(transform.position, finalPosition, speed * Time.deltaTime);
                transform.position = newPosition;
                sqrRemainingDistance = (transform.position - finalPosition).sqrMagnitude;
                yield return null;
            }

            transform.position = finalPosition;
        }

        void Start()
        {
            if (eventSystem == null)
                eventSystem = EventSystem.FindObjectOfType<EventSystem>();  

            if(alphabet.Length == 0)
            {
                var children = transform.GetComponentsInChildren<Button>();
                alphabet = children.
                Where(x => x.name.Length == 1 && !Char.IsNumber(x.name, 0)).                
                ToArray();
            }            
        } 

        public void ToggleCapsLock()
        {
            capsLock = !capsLock;
            foreach (var button in alphabet)
            {
                var textComponent = button.GetComponentInChildren<Text>();
                textComponent.text = (capsLock == true) ? textComponent.text.ToUpper() :
                                                        textComponent.text.ToLower();
            }
        }

        public void ToggleVirtualKeyboard(bool value)
        {
            if (value && !ActiveInScene)
                StartCoroutine(SmoothMovement(visiblePosition));
            else if (!value && ActiveInScene)
                StartCoroutine(SmoothMovement(hiddenPosition));
            else return;

            ActiveInScene = value;
        }

        public void TurnDragOnOff(bool value)
        {
            isDraggable = value;
        }

        void Update()
        {
            if (isDraggable)
                oldMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            if(Input.GetMouseButtonDown(0))
            {                
                bool logic = eventSystem.currentSelectedGameObject?.GetComponent<InputField>();          
                if (logic)
                {
                    if (!ActiveInScene) ToggleVirtualKeyboard(true);
                    SelectObject();
                    SelectInputField();
                }
            }
        }
    }
}