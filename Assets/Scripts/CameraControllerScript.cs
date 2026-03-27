using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraControllerScript : MonoBehaviour
{
    public static CameraControllerScript instance;


    [Header("General")]
    [SerializeField] Transform cameraTransform;
    public Transform followTransform;
    Vector3 newPosition;
    Vector3 dragStartPosition;
    Vector3 dragCurrentPosition;

    [Header("Optional Functionality")]
    [SerializeField] bool MoveWithKeyboardInput;
    [SerializeField] bool moveWithEdgeScrolling;
    [SerializeField] bool moveWithMouseDrag;

    [SerializeField] bool zoomWithMouseScroll;

     [SerializeField] bool _isVerrticalRotationEnabled; // hatalı çalışabilir kapa/aç butonu eklenecek, test için ekibe video gönder;

    [Header("Keyboard Movement")]
    [SerializeField] float fastSpeed = 0.05f;
    [SerializeField] float normalSpeed = 0.01f;
    [SerializeField] float movementSensitivity = 1f;
    float movementSpeed;

    [Header("Edge Scrolling Movement")]
    [SerializeField] float edgeSize = 50f;
    bool isCursorSet = false;
    public Texture2D cursorArrowUp;
    public Texture2D cursorArrowDown;
    public Texture2D cursorArrowLeft;
    public Texture2D cursorArrowRight;

     [Header("Mouse Rotation") ]

    private float _mouseY;
    private float _mouseX;
   
    private float _maxVerticalRotation = 65f;
    private float _minVerticalRotation = 0f;
   
   
   
    CursorArrow currentCursor = CursorArrow.DEFAULT;
    enum CursorArrow
    {
        UP,
        DOWN,
        LEFT,
        RIGHT,
        DEFAULT
    }

    private void Start()
    {
        instance = this;

        newPosition = transform.position;

        movementSpeed = normalSpeed;
    }

    

    private void Update()
    {

        if (followTransform != null)
        {
            transform.position = followTransform.position;
        }

        else
        {
            HandleCameraMovement();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            followTransform = null;
        }

        if (Input.GetKey(KeyCode.F))
        {
            moveWithEdgeScrolling = false;
        }
        if (Input.GetKey(KeyCode.G))
        {
            moveWithEdgeScrolling = true;
        }
    }

    void HandleCameraMovement()
    {
         HandleMouseRotation();
        _mouseX = Input.mousePosition.x; // Camera Rotation için gerekli inputlar
        _mouseY = Input.mousePosition.y;


        if (moveWithMouseDrag)
        {
            HandleMouseDragInput();
        }


        if (MoveWithKeyboardInput)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                movementSpeed = fastSpeed;
            }
            else
            {
                movementSpeed = normalSpeed;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                newPosition += (transform.forward * movementSpeed);
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                newPosition += (transform.forward * -movementSpeed);
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                newPosition += (transform.right * movementSpeed);
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                newPosition += (transform.right * -movementSpeed);
            }
        }

        if (zoomWithMouseScroll)
        {
            Camera.main.fieldOfView -= Input.mouseScrollDelta.y * 5f;
            Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView, 15f, 50f); // zoom limitlerini belirler ,değiştirilebilir
        }
        if (moveWithEdgeScrolling)
        {

            //edge scrolling bi tık sinir bozucu olabilir, ama şu an için bu şekilde çalışıyor
            if (Input.mousePosition.x > Screen.width - edgeSize)
            {
                newPosition += (transform.right * movementSpeed);
                ChangeCursor(CursorArrow.RIGHT);
                isCursorSet = true;
            }

            else if (Input.mousePosition.x < edgeSize)
            {
                newPosition += (transform.right * -movementSpeed);
                ChangeCursor(CursorArrow.LEFT);
                isCursorSet = true;
            }


            else if (Input.mousePosition.y > Screen.height - edgeSize)
            {
                newPosition += (transform.forward * movementSpeed);
                ChangeCursor(CursorArrow.UP);
                isCursorSet = true;
            }


            else if (Input.mousePosition.y < edgeSize)
            {
                newPosition += (transform.forward * -movementSpeed);
                ChangeCursor(CursorArrow.DOWN);
                isCursorSet = true;
            }
            else
            {
                if (isCursorSet)
                {
                    ChangeCursor(CursorArrow.DEFAULT);
                    isCursorSet = false;
                }
            }
        }

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementSensitivity);

        Cursor.lockState = CursorLockMode.Confined;
    }

    private void ChangeCursor(CursorArrow newCursor)
    {

        if (currentCursor != newCursor)
        {
            switch (newCursor)
            {
                case CursorArrow.UP:
                    Cursor.SetCursor(cursorArrowUp, Vector2.zero, CursorMode.Auto);
                    break;
                case CursorArrow.DOWN:
                    Cursor.SetCursor(cursorArrowDown, new Vector2(cursorArrowDown.width, cursorArrowDown.height), CursorMode.Auto);
                    break;
                case CursorArrow.LEFT:
                    Cursor.SetCursor(cursorArrowLeft, Vector2.zero, CursorMode.Auto);
                    break;
                case CursorArrow.RIGHT:
                    Cursor.SetCursor(cursorArrowRight, new Vector2(cursorArrowRight.width, cursorArrowRight.height), CursorMode.Auto);
                    break;
                case CursorArrow.DEFAULT:
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    break;
            }

            currentCursor = newCursor;
        }
    }



    private void HandleMouseDragInput()
    {
        if (Input.GetMouseButtonDown(2) && EventSystem.current.IsPointerOverGameObject() == false)
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float entry;

            if (plane.Raycast(ray, out entry))
            {
                dragStartPosition = ray.GetPoint(entry);
            }
        }
        if (Input.GetMouseButton(2) && EventSystem.current.IsPointerOverGameObject() == false)
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float entry;

            if (plane.Raycast(ray, out entry))
            {
                dragCurrentPosition = ray.GetPoint(entry);

                newPosition = transform.position + dragStartPosition - dragCurrentPosition;
            }
        }
    }
    private void HandleMouseRotation()
    {
        var handleFactor = 10f;
        if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.mousePosition.x != _mouseX)
            {
                var cameraRotationY = (Input.mousePosition.x - _mouseX) * handleFactor * Time.deltaTime;
                transform.Rotate(0f, cameraRotationY, 0f);
            }
            }
          if(Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftAlt))
        {  if(_isVerrticalRotationEnabled && Input.mousePosition.y != _mouseY)
        {
            GameObject MainCamera = this.gameObject.transform.Find("Main Camera").gameObject;
            var cameraRotationX = (_mouseY - Input.mousePosition.y) * handleFactor * Time.deltaTime;           
           var wantedRotationX = MainCamera.transform.localEulerAngles.x + cameraRotationX;

            if (wantedRotationX <= _maxVerticalRotation && wantedRotationX >= _minVerticalRotation)
            {
                MainCamera.transform.Rotate(cameraRotationX, -0f, 0f);
            }
            MainCamera.transform.localEulerAngles = new Vector3(wantedRotationX , transform.localEulerAngles.y, MainCamera.transform.localEulerAngles.z);
        }}
    }
}
