using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

    #region PUBLIC VARIABLES

    #region EDITOR VARIABLES

    public bool keyboardAndMouse;

    #endregion


    #region CONTROLLER PUBLIC VARIABLES

    public float playerScale = 0.1f; //0.1f
    public float cameraSensitivityX = 150f; //250f
    public float cameraSensitivityY = 150f; //250f
    public float skydivingSpeedMultiplier = 5f; //5f
    public float sprintSpeedMultiplier = 1.5f; //1.5f
    public float runningSpeed = 35f; //35f
    public float crouchingSpeed = 22f; //22f
    public float jumpHeight = 50f; //50f
    public float gravity = 5f; //5f
    public float maxSkydivingGravity = 3.5f; //3f
    public float minSkydivingGravity = 2f; //1.5f

    #endregion

    #endregion

    #region PRIVATE VARIABLES

    #region CONTROLLER PRIVATE VARIABLES

    private float joystickHandleRadius = 60f; //60f
    private float fallDistanceThreshold = 8f; //8f
    private float buttonsEdgeDiameter = 35f; //35f
    private float panYLimit = 60f; // 60f
    private float verticalScreenDividerOffset = -200f; //-200f
    private float jumpDuration = 0.5f; //0.5f
    private float standHeightLimit = 4f; //4f
    private float cameraColliderRadius = 1f; //1f
    private float cameraProximityRadiusOffset = 2f; //2f
    private float minCameraDistance = 7f; //7f
    private float maxCameraDistance = 33f; //33f
    private float cameraAdjustmentSpeed = 75f; //75f
    private float isGroundedCheckRadius = 4f; //4f
    private float gravityCheckWidth = 0.4f; //0.4f
    private float sprintFingerDistance = 225f; //225f
    private float sprintSpriteFingerOffset = 75f; //75f
    private float sprintSpriteJoystickOffset = 35f; //35f
    private float sprintSpriteAlphaMin = 0.4f; //0.4f
    private float sprintAnimSpeed = 1.25f; //1.25f
    private float terminalGravityAccel = 50f; //50f  //Update if Changed --> Animator --> Fall/BlendTree
    private float skydiveCameraTilt = 5f; //10f
    private float lerpSpeed = 15f; //15f
    private float gravityAccelerationIndex = 0.5f; //0.5f

    //Character Controller Dimensions
    private Vector2 standingColliderParam = new Vector2(9f, 14f); // Vector2(center y offset, height) = Vector2(9f, 14f)
    private Vector2 crouchingColliderParam = new Vector2(6.73f, 9.5f); // Vector2(center y offset, height) = Vector2(6.73f, 9.5f)

    #endregion

    #region MISCELLANEOUS VARIABLES

    private float verticalAxis = 0f, horizontalAxis = 0f;
    private Vector2 panAxis = Vector2.zero;

    private CharacterController charController;

    private Transform camLock, cam, player;
    private RectTransform joystick, joystickHandle, joystickSprint;
    private RectTransform crouchDownUnpressed, crouchDownPressed, crouchUpUnpressed, crouchUpPressed;
    private RectTransform jumpUnpressed, jumpPressed;

    private float halfWidth, halfHeight;
    private float crouchButtonMinX, crouchButtonMaxX, crouchButtonMinY, crouchButtonMaxY;
    private float jumpButtonMinX, jumpButtonMaxX, jumpButtonMinY, jumpButtonMaxY;    
    private float gravityAccel = 0f;
    private float initCamLocalEulerXAngle;
    private float initPlayerEulerYAngle;
    private float cumulPanYAxis = 0f;
    private float camDeltaXAngleLimit;
    private float fallDistance = 0f;
    private float jumpElapsedTime = 0f;    
    private float maxAxis;    
    private float deltaTilt;

    private Touch joystickTouch, crouchButtonTouch, jumpButtonTouch, initialPanTouch;

    private bool crouch = false, jumping = false, skydiving = false, skydivingDone = true;

    #endregion

    #endregion


    //This Will Run Once at the Start
    #region Start
    void Start() {
        
        //SHOULD UPDATE WHEN SENSITIVITY IS CHANGED IN SETTINGS MENU
        camDeltaXAngleLimit = 1f / (cameraSensitivityY / panYLimit);

        //Assigning Objects Accordingly
        cam = Camera.main.transform;
        charController = GetComponent<CharacterController>();
        for (int i = 0; i < transform.childCount; i++) {
            if (transform.GetChild(i).tag == "PlayerModel")
                player = transform.GetChild(i);
            else if (transform.GetChild(i).tag == "Canvas") {
                transform.GetChild(i).GetComponent<CanvasScaler>().referenceResolution = Screen.safeArea.size;
                for (int j = 0; j < transform.GetChild(i).childCount; j++) {
                    if (transform.GetChild(i).GetChild(j).tag == "JoystickBase")
                        joystick = transform.GetChild(i).GetChild(j).GetComponent<RectTransform>();
                    else if (transform.GetChild(i).GetChild(j).tag == "JoystickHandle")
                        joystickHandle = transform.GetChild(i).GetChild(j).GetComponent<RectTransform>();
                    else if (transform.GetChild(i).GetChild(j).tag == "JoystickSprint")
                        joystickSprint = transform.GetChild(i).GetChild(j).GetComponent<RectTransform>();
                    else if (transform.GetChild(i).GetChild(j).tag == "CrouchDownUnpressed")
                        crouchDownUnpressed = transform.GetChild(i).GetChild(j).GetComponent<RectTransform>();
                    else if (transform.GetChild(i).GetChild(j).tag == "CrouchDownPressed")
                        crouchDownPressed = transform.GetChild(i).GetChild(j).GetComponent<RectTransform>();
                    else if (transform.GetChild(i).GetChild(j).tag == "CrouchUpUnpressed")
                        crouchUpUnpressed = transform.GetChild(i).GetChild(j).GetComponent<RectTransform>();
                    else if (transform.GetChild(i).GetChild(j).tag == "CrouchUpPressed")
                        crouchUpPressed = transform.GetChild(i).GetChild(j).GetComponent<RectTransform>();
                    else if (transform.GetChild(i).GetChild(j).tag == "JumpUnpressed")
                        jumpUnpressed = transform.GetChild(i).GetChild(j).GetComponent<RectTransform>();
                    else if (transform.GetChild(i).GetChild(j).tag == "JumpPressed")
                        jumpPressed = transform.GetChild(i).GetChild(j).GetComponent<RectTransform>();
                }
            }
        }
        for (int i = 0; i < player.childCount; i++) {
            if (player.GetChild(i).tag == "CameraLock")
                camLock = player.GetChild(i);
        }

        //Setting Player Scale
        player.localScale = Vector3.one * playerScale;
        player.parent.localScale = Vector3.one;

        //Adjusting Constants According to Player Scale
        runningSpeed *= playerScale;
        crouchingSpeed *= playerScale;
        jumpHeight *= playerScale;
        fallDistanceThreshold *= playerScale;
        standHeightLimit *= playerScale;
        cameraColliderRadius *= playerScale;
        cameraProximityRadiusOffset *= playerScale;
        minCameraDistance *= playerScale;
        maxCameraDistance *= playerScale;
        isGroundedCheckRadius *= playerScale;
        gravityCheckWidth *= playerScale;
        standingColliderParam *= playerScale;
        crouchingColliderParam *= playerScale;
        gravity *= playerScale;
        minSkydivingGravity *= playerScale;
        maxSkydivingGravity *= playerScale;
        cameraAdjustmentSpeed *= playerScale;

        charController.radius = 1f * playerScale; //Default Radius = 1f
        charController.stepOffset = 4f * playerScale; //Default Step Offset = 4f
        charController.skinWidth = 3f * playerScale; //Default Skin Width = 3f

        //Setting Additional Character Controller Constants
        charController.slopeLimit = 50f; //Default Slope Limit = 35f

        //Setting Collider to Standing Position
        charController.center = new Vector3(charController.center.x, standingColliderParam.x, charController.center.z);
        charController.height = standingColliderParam.y;

        //Setting Camera Collider Proximity Radius
        cameraProximityRadiusOffset += cameraColliderRadius;

        //Setting intial Pan Rotation
        initPlayerEulerYAngle = player.parent.eulerAngles.y;
        initCamLocalEulerXAngle = camLock.localEulerAngles.x;

        //Showing and Hiding UI Accordingly
        joystick.gameObject.SetActive(false);
        joystickHandle.gameObject.SetActive(false);
        joystickSprint.gameObject.SetActive(false);

        crouchDownUnpressed.gameObject.SetActive(true);
        crouchDownPressed.gameObject.SetActive(false);
        crouchUpUnpressed.gameObject.SetActive(false);
        crouchUpPressed.gameObject.SetActive(false);

        jumpUnpressed.gameObject.SetActive(true);
        jumpPressed.gameObject.SetActive(false);

        //Saving Half Resolution for Later Use
        halfHeight = Screen.height / 2;
        halfWidth = Screen.width / 2;
        

        //Re-scaling and Re-positioning UI According to Current Screen Resolution
        joystick.localScale = new Vector2(joystick.localScale.y * Screen.height / 480, joystick.localScale.y * Screen.height / 480);
        joystickHandle.localScale = new Vector2(joystickHandle.localScale.y * Screen.height / 480, joystickHandle.localScale.y * Screen.height / 480);
        joystickSprint.localScale = new Vector2(joystickSprint.localScale.x * Screen.width / 800, joystickSprint.localScale.y * Screen.height / 480);

        crouchDownUnpressed.localScale = new Vector2(crouchDownUnpressed.localScale.y * Screen.height / 480, crouchDownUnpressed.localScale.y * Screen.height / 480);
        crouchDownPressed.localScale = new Vector2(crouchDownPressed.localScale.y * Screen.height / 480, crouchDownPressed.localScale.y * Screen.height / 480);
        crouchUpUnpressed.localScale = new Vector2(crouchUpUnpressed.localScale.y * Screen.height / 480, crouchUpUnpressed.localScale.y * Screen.height / 480);
        crouchUpPressed.localScale = new Vector2(crouchUpPressed.localScale.y * Screen.height / 480, crouchUpPressed.localScale.y * Screen.height / 480);

        jumpUnpressed.localScale = new Vector2(jumpUnpressed.localScale.y * Screen.height / 480, jumpUnpressed.localScale.y * Screen.height / 480);
        jumpPressed.localScale = new Vector2(jumpPressed.localScale.y * Screen.height / 480, jumpPressed.localScale.y * Screen.height / 480);


        crouchDownUnpressed.localPosition = new Vector3((crouchDownUnpressed.localPosition.x * Screen.width) / 800,
            (crouchDownUnpressed.localPosition.y * Screen.height) / 480, 0f);
        crouchDownPressed.localPosition = new Vector3((crouchDownPressed.localPosition.x * Screen.width) / 800,
            (crouchDownPressed.localPosition.y * Screen.height) / 480, 0f);
        crouchUpUnpressed.localPosition = new Vector3((crouchUpUnpressed.localPosition.x * Screen.width) / 800,
            (crouchUpUnpressed.localPosition.y * Screen.height) / 480, 0f);
        crouchUpPressed.localPosition = new Vector3((crouchUpPressed.localPosition.x * Screen.width) / 800,
            (crouchUpPressed.localPosition.y * Screen.height) / 480, 0f);

        
        jumpUnpressed.localPosition = new Vector3((jumpUnpressed.localPosition.x * Screen.width) / 800,
            (jumpUnpressed.localPosition.y * Screen.height) / 480, 0f);
        jumpPressed.localPosition = new Vector3((jumpPressed.localPosition.x * Screen.width) / 800,
            (jumpPressed.localPosition.y * Screen.height) / 480, 0f);


        //Assigning Constants Accroding to new Scales and Positions
        joystickHandleRadius = (joystickHandleRadius * Screen.height) / 480;

        sprintFingerDistance = (sprintFingerDistance * Screen.height) / 480;

        sprintSpriteFingerOffset = (sprintSpriteFingerOffset * Screen.height) / 480;

        sprintSpriteJoystickOffset = (sprintSpriteJoystickOffset * Screen.height) / 480;

        verticalScreenDividerOffset = (verticalScreenDividerOffset * Screen.width) / 800f;

        crouchButtonMinX = crouchDownUnpressed.localPosition.x - ((buttonsEdgeDiameter * Screen.width) / 800);
        crouchButtonMaxX = crouchDownUnpressed.localPosition.x + ((buttonsEdgeDiameter * Screen.width) / 800);
        crouchButtonMinY = crouchDownUnpressed.localPosition.y - ((buttonsEdgeDiameter * Screen.width) / 800);
        crouchButtonMaxY = crouchDownUnpressed.localPosition.y + ((buttonsEdgeDiameter * Screen.width) / 800);

        jumpButtonMinX = jumpUnpressed.localPosition.x - ((buttonsEdgeDiameter * Screen.width) / 800);
        jumpButtonMaxX = jumpUnpressed.localPosition.x + ((buttonsEdgeDiameter * Screen.width) / 800);
        jumpButtonMinY = jumpUnpressed.localPosition.y - ((buttonsEdgeDiameter * Screen.width) / 800);
        jumpButtonMaxY = jumpUnpressed.localPosition.y + ((buttonsEdgeDiameter * Screen.width) / 800);

        
    }
    #endregion

    //This Will Run Once Every Frame
    #region Update
    void Update() {
        
        #region PC
        if (keyboardAndMouse) {

            //Find Actual Falling Distance
            float realFallDist = -fallDistance;

            //Keyboard Buttons Handler
            if (Input.GetKeyDown(KeyCode.C)) {

                if (crouch && isAbleToStand(true)) {

                    crouch = false;
                    charController.center = new Vector3(charController.center.x, standingColliderParam.x, charController.center.z);
                    charController.height = standingColliderParam.y;

                } else {

                    crouch = true;
                    charController.center = new Vector3(charController.center.x, crouchingColliderParam.x, charController.center.z);
                    charController.height = crouchingColliderParam.y;

                }

            } else if (Input.GetKeyDown(KeyCode.Space) && IsGrounded() && isAbleToStand(false) && isAbleToStand(true) && realFallDist <= isGroundedCheckRadius)
                jumping = true;

            //Check if Skydiving
            skydiving = gravityAccel >= terminalGravityAccel && realFallDist > fallDistanceThreshold;

            //Assign Each Axis
            horizontalAxis = Input.GetAxis("Horizontal");
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Vertical") > 0f && !skydiving)
                verticalAxis = sprintSpeedMultiplier;
            else
                verticalAxis = Input.GetAxis("Vertical");

            maxAxis = Mathf.Max(Mathf.Abs(verticalAxis), Mathf.Abs(horizontalAxis));

            //Affect Player by Gravity
            gravityEffect();

            
            //Send Parameters to Animator
            Animator ani = player.GetComponent<Animator>();
            ani.SetFloat("vertical", verticalAxis);
            ani.SetFloat("horizontal", horizontalAxis);
            ani.SetFloat("fall", realFallDist / playerScale);
            ani.SetBool("crouch", crouch);
            ani.SetBool("jump", jumping);
            ani.SetFloat("runSprintSpeed", (verticalAxis > 1 && !skydiving) ? ((verticalAxis * sprintAnimSpeed) / sprintSpeedMultiplier) : 1f); 
            ani.SetFloat("gravityAcceleration", gravityAccel);
            ani.SetFloat("skydiveSpeed", maxAxis);


            //Move Player
            charController.Move((player.forward * verticalAxis + player.right * horizontalAxis) * (crouch ? crouchingSpeed : runningSpeed) * Time.deltaTime);
            

            //Stop Crouching if Falling
            if (realFallDist > fallDistanceThreshold) {

                crouch = false;
                charController.center = new Vector3(charController.center.x, standingColliderParam.x, charController.center.z);
                charController.height = standingColliderParam.y;

            }

            //Jump Handler
            jumpHandler();

            //Camera Collision/Occlusion Detection and Handling
            cameraCollisionOcclusionHandler(crouch);

            #region Panning Handler (custom for Mouse)
            panAxis = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
            
            player.parent.eulerAngles += new Vector3(0f, (panAxis.x * cameraSensitivityX / 35f), 0f);
            
            if (!skydiving || (skydiving && maxAxis == 0f)) {

                float newAngle = camLock.localEulerAngles.x + (panAxis.y * cameraSensitivityY / 35f);
                
                if (newAngle > panYLimit && newAngle < 360f - panYLimit) {
                    if (newAngle >= 180f)
                        newAngle = 360f - panYLimit;
                    else
                        newAngle = panYLimit;
                }

                camLock.localEulerAngles = new Vector3(newAngle, 0f, 0f);
                player.parent.eulerAngles = new Vector3(0f, player.parent.eulerAngles.y, 0f);



            } else {

                float newAngle = player.parent.eulerAngles.x + (panAxis.y * cameraSensitivityY / 35f);

                if (newAngle > panYLimit && newAngle < 360f - panYLimit) {
                    if (newAngle >= 180f)
                        newAngle = 360f - panYLimit;
                    else
                        newAngle = panYLimit;
                }

                newAngle = Mathf.Clamp(newAngle, 0f, 60f);

                
                float deltaTilt = horizontalAxis * skydiveCameraTilt;
                player.parent.eulerAngles = new Vector3(newAngle, player.parent.eulerAngles.y, -deltaTilt);
                camLock.localEulerAngles = new Vector3(0f, 0f, deltaTilt);

            }
            #endregion


            return;
        }
        #endregion

        #region ANDROID

        //Find the Actual Fall Distance 
        float realFallDistance = -fallDistance;

        //Joystick Touch Input Handler
        joystickHandler();

        //Check if Skydiving
        skydiving = gravityAccel >= terminalGravityAccel && realFallDistance > fallDistanceThreshold;

        #region Assigning Vertical and Horizontal Axis
        if (joystickTouch.position != Vector2.zero) {

            horizontalAxis = (joystickHandle.localPosition.x - joystick.localPosition.x) / joystickHandleRadius;

            if (crouch) {

                joystickSprint.gameObject.SetActive(false);
                verticalAxis = (joystickHandle.localPosition.y - joystick.localPosition.y) / joystickHandleRadius;

            } else {
                          
                if (joystickHandle.localPosition.y >= joystick.localPosition.y 
                    && joystickTouch.position.y - halfHeight - joystick.localPosition.y > joystickHandleRadius
                    && horizontalAxis < 0.5f
                    && !skydiving) {

                    joystickSprint.gameObject.SetActive(true);

                    verticalAxis = 1f + (((joystickTouch.position.y - halfHeight + sprintSpriteFingerOffset - joystickHandle.localPosition.y) * (sprintSpeedMultiplier - 1f)) / sprintFingerDistance);
                    verticalAxis = Mathf.Clamp(verticalAxis, -1f, sprintSpeedMultiplier);

                    joystickSprint.localPosition = new Vector3(joystick.localPosition.x,
                        joystickTouch.position.y - halfHeight - joystick.localPosition.y <= sprintFingerDistance ?
                        joystickTouch.position.y - halfHeight + sprintSpriteFingerOffset 
                        : joystick.localPosition.y + sprintFingerDistance + sprintSpriteFingerOffset, 0f);
                    joystickSprint.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 
                        sprintSpriteAlphaMin + (((verticalAxis - 1f) * (1f - sprintSpriteAlphaMin)) / (sprintSpeedMultiplier - 1f)));

                } else if (!skydiving) {

                    joystickSprint.gameObject.SetActive(true);
                    verticalAxis = (joystickHandle.localPosition.y - joystick.localPosition.y) / joystickHandleRadius;
                    joystickSprint.localPosition = new Vector3(joystick.localPosition.x, joystick.localPosition.y + joystickHandleRadius + sprintSpriteJoystickOffset, 0f);
                    joystickSprint.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, sprintSpriteAlphaMin);

                } else {

                    joystickSprint.gameObject.SetActive(false);
                    verticalAxis = (joystickHandle.localPosition.y - joystick.localPosition.y) / joystickHandleRadius;

                }                         
            }            

        } else {

            joystickSprint.gameObject.SetActive(false);

            if (!skydiving) {
                verticalAxis = 0f;
                horizontalAxis = 0f;
            }
            
        }
        #endregion

        //Crouch Button Touch Input Handler
        crouchButtonHandler();

        //Jump Button Touch Input Handler
        jumpButtonHandler();
               


        #region Identifying Touches
        if (Input.touchCount > 0) {
            for (int i = 0; i < Input.touchCount; i++) {

                if (Input.GetTouch(i).position.x < halfWidth + verticalScreenDividerOffset) {                               //Left side Touch

                    //Pan Touch Cross Over
                    if(initialPanTouch.position != Vector2.zero && Input.GetTouch(i).fingerId == initialPanTouch.fingerId){
                        
                        panAxis = Vector2.zero;
                        initialPanTouch.position = Vector2.zero;
                        initPlayerEulerYAngle = player.parent.eulerAngles.y;
                        
                    }
                    
                    if (Input.GetTouch(i).phase == TouchPhase.Began ||
                        (joystickTouch.position != Vector2.zero && Input.GetTouch(i).fingerId == joystickTouch.fingerId)) {
                    
                        //Assinging Joystick Touch
                        joystickTouch = Input.GetTouch(i);
                    }

                } else {                                                                      //Right side touch

                    //Pan Touch
                    if (Input.GetTouch(i).phase == TouchPhase.Began) {

                        initialPanTouch = Input.GetTouch(i);

                    } else if (Input.GetTouch(i).phase == TouchPhase.Ended || Input.GetTouch(i).phase == TouchPhase.Canceled) {

                        if (joystickTouch.position != Vector2.zero && Input.GetTouch(i).fingerId == joystickTouch.fingerId) {

                            joystickTouch.position = Vector2.zero;
                            continue;

                        }

                        panAxis = Vector2.zero;
                        initialPanTouch.position = Vector2.zero;
                        initPlayerEulerYAngle = player.parent.eulerAngles.y;

                    } else {

                        if (joystickTouch.position != Vector2.zero && Input.GetTouch(i).fingerId == joystickTouch.fingerId) {

                            joystickTouch = Input.GetTouch(i);
                            continue;

                        }

                        if (initialPanTouch.position == Vector2.zero)
                            continue;

                        panAxis.x = (Input.GetTouch(i).position.x - initialPanTouch.position.x) / halfWidth;
                        panAxis.y = -Input.GetTouch(i).deltaPosition.y / halfHeight;

                    }

                    //Crouch Button Touched 
                    Vector3 touchPos = Input.GetTouch(i).position;
                    touchPos.x -= halfWidth;
                    touchPos.y -= halfHeight;
                    touchPos.z = 0f;
                    if (touchPos.x > crouchButtonMinX && touchPos.x < crouchButtonMaxX
                        && touchPos.y > crouchButtonMinY && touchPos.y < crouchButtonMaxY) {
                        if (crouchButtonTouch.position != Vector2.zero || Input.GetTouch(i).phase == TouchPhase.Began)
                            crouchButtonTouch = Input.GetTouch(i);
                    } else if (crouchButtonTouch.position != Vector2.zero &&
                        (Input.GetTouch(i).phase == TouchPhase.Moved || Input.GetTouch(i).phase == TouchPhase.Stationary)) {

                        if (!crouch) {
                            crouchDownUnpressed.gameObject.SetActive(true);
                            crouchDownPressed.gameObject.SetActive(false);
                            crouchUpUnpressed.gameObject.SetActive(false);
                            crouchUpPressed.gameObject.SetActive(false);
                        } else {
                            crouchDownUnpressed.gameObject.SetActive(false);
                            crouchDownPressed.gameObject.SetActive(false);
                            crouchUpUnpressed.gameObject.SetActive(true);
                            crouchUpPressed.gameObject.SetActive(false);
                        }
                        crouchButtonTouch.position = Vector2.zero;
                    }

                    //Jump Button Touched 
                    else if (touchPos.x > jumpButtonMinX && touchPos.x < jumpButtonMaxX
                        && touchPos.y > jumpButtonMinY && touchPos.y < jumpButtonMaxY) {

                        if (jumpButtonTouch.position != Vector2.zero || Input.GetTouch(i).phase == TouchPhase.Began)
                            jumpButtonTouch = Input.GetTouch(i);

                    } else if (jumpButtonTouch.position != Vector2.zero &&
                        (Input.GetTouch(i).phase == TouchPhase.Moved || Input.GetTouch(i).phase == TouchPhase.Stationary)) {

                        jumpUnpressed.gameObject.SetActive(true);
                        jumpPressed.gameObject.SetActive(false);
                        jumpButtonTouch.position = Vector2.zero;

                    }
                }
            }
        } else {
            joystickTouch.position = Vector2.zero;
            crouchButtonTouch.position = Vector2.zero;
            jumpButtonTouch.position = Vector2.zero;
        }
        #endregion


        maxAxis = Mathf.Max(Mathf.Abs(verticalAxis > 0 ? verticalAxis : 0f), Mathf.Abs(horizontalAxis));

        //Affect Player by Gravity
        gravityEffect();

        //Sending Animator Controller Parameters
        Animator anim = player.GetComponent<Animator>();
        anim.SetFloat("vertical", verticalAxis);
        anim.SetFloat("horizontal", horizontalAxis);
        anim.SetFloat("fall", realFallDistance / playerScale);
        anim.SetBool("crouch", crouch);
        anim.SetBool("jump", jumping);
        anim.SetFloat("runSprintSpeed", verticalAxis > 1 ? ((verticalAxis * sprintAnimSpeed) / sprintSpeedMultiplier) : 1f);
        anim.SetFloat("gravityAcceleration", gravityAccel);
        anim.SetFloat("skydiveSpeed", maxAxis);


        //Player Movement
        charController.Move((player.forward * verticalAxis + player.right * horizontalAxis) * (crouch ? crouchingSpeed : runningSpeed) * Time.deltaTime);
        

        //If Falling Stop Crouching
        if (realFallDistance > fallDistanceThreshold) {
            crouchDownUnpressed.gameObject.SetActive(true);
            crouchDownPressed.gameObject.SetActive(false);
            crouchUpUnpressed.gameObject.SetActive(false);
            crouchUpPressed.gameObject.SetActive(false);
            crouch = false;
            charController.center = new Vector3(charController.center.x, standingColliderParam.x, charController.center.z);
            charController.height = standingColliderParam.y;
        }

        //Player Panning
        panningHandler();

        //Jump Handler
        jumpHandler();

        //Camera Collision/Occlusion Detection and Handling
        cameraCollisionOcclusionHandler(crouch);
        
        #endregion

    }
    #endregion


    #region PUBLIC METHODS

    #region getVerticalAxis
    public float getVerticalAxis() {
        return verticalAxis;
    }
    #endregion
    #region getHorizontalAxis
    public float getHorizontalAxis() {
        return horizontalAxis;
    }
    #endregion
    #region getFallDistance
    public float getFallDistance() {
        return -fallDistance;
    }
    #endregion
    #region IsCrouching
    public bool IsCrouching() {
        return crouch;
    }
    #endregion
    #region IsJumping
    public bool IsJumping() {
        return jumping;
    }
    #endregion
    #region IsGrounded
    public bool IsGrounded() {
        return Physics.CheckSphere(player.position, isGroundedCheckRadius, LayerMask.GetMask("Ground"));
    }
    #endregion

    #endregion

    #region PRIVATE METHODS
    
    #region crouchButtonHandler
    private void crouchButtonHandler() {
        if (crouchButtonTouch.position != Vector2.zero) {
            
            if (crouchButtonTouch.phase == TouchPhase.Began) {
                if (!crouch) {
                    crouchDownUnpressed.gameObject.SetActive(false);
                    crouchDownPressed.gameObject.SetActive(true);
                    crouchUpUnpressed.gameObject.SetActive(false);
                    crouchUpPressed.gameObject.SetActive(false);
                } else {
                    crouchDownUnpressed.gameObject.SetActive(false);
                    crouchDownPressed.gameObject.SetActive(false);
                    crouchUpUnpressed.gameObject.SetActive(false);
                    crouchUpPressed.gameObject.SetActive(true);
                }
            } else if (crouchButtonTouch.phase == TouchPhase.Ended || crouchButtonTouch.phase == TouchPhase.Canceled) {

                Vector3 touchPos = crouchButtonTouch.position;
                touchPos.x -= halfWidth;
                touchPos.y -= halfHeight;
                touchPos.z = 0f;

                if (touchPos.x > crouchButtonMinX && touchPos.x < crouchButtonMaxX
                    && touchPos.y > crouchButtonMinY && touchPos.y < crouchButtonMaxY) {
                    if (crouch && isAbleToStand(true)) {
                        crouchDownUnpressed.gameObject.SetActive(true);
                        crouchDownPressed.gameObject.SetActive(false);
                        crouchUpUnpressed.gameObject.SetActive(false);
                        crouchUpPressed.gameObject.SetActive(false);
                        crouch = false;
                        charController.center = new Vector3(charController.center.x, standingColliderParam.x, charController.center.z);
                        charController.height = standingColliderParam.y;
                    } else {
                        crouchDownUnpressed.gameObject.SetActive(false);
                        crouchDownPressed.gameObject.SetActive(false);
                        crouchUpUnpressed.gameObject.SetActive(true);
                        crouchUpPressed.gameObject.SetActive(false);
                        crouch = true;
                        charController.center = new Vector3(charController.center.x, crouchingColliderParam.x, charController.center.z);
                        charController.height = crouchingColliderParam.y;
                    }
                }
                crouchButtonTouch.position = Vector2.zero;
            }
        }
    }
    #endregion
    #region joystickHandler
    private void joystickHandler() {
        if (joystickTouch.position != Vector2.zero) {
            Vector3 touchPos = joystickTouch.position;
            touchPos.x -= halfWidth;
            touchPos.y -= halfHeight;
            touchPos.z = 0f;
            if (joystickTouch.phase == TouchPhase.Began) {
                joystick.gameObject.SetActive(true);
                joystickHandle.gameObject.SetActive(true);

                joystick.localPosition = touchPos;
                joystickHandle.localPosition = touchPos;
            } else if (joystickTouch.phase == TouchPhase.Ended || joystickTouch.phase == TouchPhase.Canceled) {
                joystick.gameObject.SetActive(false);
                joystickHandle.gameObject.SetActive(false);
                joystickTouch.position = Vector2.zero;
            } else if (joystickTouch.phase == TouchPhase.Moved || joystickTouch.phase == TouchPhase.Stationary) {
                joystickHandle.localPosition = touchPos;

                float distance = Vector3.Distance(joystickHandle.localPosition, joystick.localPosition);
                if (distance > joystickHandleRadius) {
                    Vector3 fromOriginToObject = joystickHandle.localPosition - joystick.localPosition;
                    fromOriginToObject *= joystickHandleRadius / distance;
                    joystickHandle.localPosition = joystick.localPosition + fromOriginToObject;
                }
            }
        } else {
            joystick.gameObject.SetActive(false);
            joystickHandle.gameObject.SetActive(false);
        }
    }
    #endregion
    #region jumpButtonHandler
    private void jumpButtonHandler() {
        if (jumpButtonTouch.position != Vector2.zero) {

            if (jumpButtonTouch.phase == TouchPhase.Began) {

                jumpUnpressed.gameObject.SetActive(false);
                jumpPressed.gameObject.SetActive(true);

            } else if (jumpButtonTouch.phase == TouchPhase.Ended || jumpButtonTouch.phase == TouchPhase.Canceled) {

                Vector3 touchPos = jumpButtonTouch.position;
                touchPos.x -= halfWidth;
                touchPos.y -= halfHeight;
                touchPos.z = 0f;

                jumpUnpressed.gameObject.SetActive(true);
                jumpPressed.gameObject.SetActive(false);
                if (touchPos.x > jumpButtonMinX && touchPos.x < jumpButtonMaxX
                    && touchPos.y > jumpButtonMinY && touchPos.y < jumpButtonMaxY) {

                    if (IsGrounded() && isAbleToStand(false) && isAbleToStand(true) && -fallDistance <= fallDistanceThreshold)
                        jumping = true;
                            
                }
                jumpButtonTouch.position = Vector2.zero;
            }
        }
    }
    #endregion
    #region panningHandler
    private void panningHandler() {

        //X Pan
        player.parent.eulerAngles = new Vector3(player.parent.eulerAngles.x, initPlayerEulerYAngle + ((panAxis.x * cameraSensitivityX)), player.parent.eulerAngles.z);

        //Y Pan
        bool skydiveAccelerated = skydiving && joystickTouch.position != Vector2.zero;

        if (!skydiving || (skydiving && !skydiveAccelerated)) {
                        
            if (skydiving) {    //YPAN NOT ACCELERATED

                /*
                //Adjusting skydiving beginning camera angle
                if (skydivingDone) {

                    float down = 0.24f * cameraSensitivityY, rDown = Mathf.Round(down);
                    if (Mathf.Round(camLock.localEulerAngles.x) != rDown 
                        || Mathf.Round(player.parent.eulerAngles.x) != 0f || Mathf.Round(cumulPanYAxis) != rDown) {

                        //Resetting camera X angle
                        camLock.localEulerAngles = new Vector3(Mathf.LerpAngle(camLock.localEulerAngles.x, down, Time.deltaTime * lerpSpeed),
                            camLock.localEulerAngles.y, camLock.localEulerAngles.z);

                        //Resetting player X angle
                        player.parent.eulerAngles = new Vector3(Mathf.LerpAngle(player.parent.eulerAngles.x, 0f, Time.deltaTime * lerpSpeed),
                            player.parent.eulerAngles.y, player.parent.eulerAngles.z);

                        //Resetting cumulative Y pan angle
                        cumulPanYAxis = Mathf.Lerp(cumulPanYAxis, down, Time.deltaTime * lerpSpeed);

                        return;

                    } else {

                        skydivingDone = false;

                    }

                }
                */

                if(skydivingDone)
                    skydivingDone = false;

                //Resetting Each Acceleration Axis if Skydiving and Not Accelerating
                verticalAxis = Mathf.Clamp(verticalAxis, -1f, 1f);
                horizontalAxis = Mathf.Clamp(horizontalAxis, -1f, 1f);

                if (verticalAxis != 0f && Mathf.Abs(verticalAxis) > 0.001f)
                    verticalAxis = Mathf.MoveTowards(verticalAxis, 0f, Time.deltaTime * 2f);
                else
                    verticalAxis = 0f;

                if (horizontalAxis != 0f && Mathf.Abs(horizontalAxis) > 0.001f)
                    horizontalAxis = Mathf.MoveTowards(horizontalAxis, 0f, Time.deltaTime * 2f);
                else
                    horizontalAxis = 0f;


                //Resetting Tilt Delta
                deltaTilt = 0f;

                //Resetting Rotations
                if (Mathf.Round(player.parent.eulerAngles.x) != 0f || Mathf.Round(player.parent.eulerAngles.z) != 0f || Mathf.Round(camLock.localEulerAngles.z) != 0f) {

                    //Resetting player X angle
                    player.parent.eulerAngles = new Vector3(Mathf.LerpAngle(player.parent.eulerAngles.x, 0f, Time.deltaTime * lerpSpeed), 
                        player.parent.eulerAngles.y, player.parent.eulerAngles.z);

                    //Setting camera X angle (Previous Transition)
                    camLock.localEulerAngles = new Vector3(Mathf.LerpAngle(camLock.localEulerAngles.x, cumulPanYAxis * cameraSensitivityY, Time.deltaTime * lerpSpeed),
                        camLock.localEulerAngles.y, camLock.localEulerAngles.z);

                    //Resetting player Z angle (Tilt)
                    player.parent.eulerAngles = new Vector3(player.parent.eulerAngles.x, player.parent.eulerAngles.y,
                        Mathf.LerpAngle(player.parent.eulerAngles.z, 0f, Time.deltaTime * lerpSpeed));

                    //Resetting camera Z angle (Tilt)
                    camLock.localEulerAngles = new Vector3(camLock.localEulerAngles.x, camLock.localEulerAngles.y,
                        Mathf.LerpAngle(camLock.localEulerAngles.z, 0f, Time.deltaTime * lerpSpeed));

                    return;

                }

                cumulPanYAxis += panAxis.y;
                cumulPanYAxis = Mathf.Clamp(cumulPanYAxis, -camDeltaXAngleLimit, camDeltaXAngleLimit);


                camLock.localEulerAngles = new Vector3(cumulPanYAxis * cameraSensitivityY, 0f, 0f);
                player.parent.eulerAngles = new Vector3(0f, player.parent.eulerAngles.y, 0f);

            } else {    //Normal Mode (Running and NOT Skydiving)


                //Adjusting skydiving ending camera angle
                if (!skydivingDone) {

                    if (Mathf.Round(camLock.localEulerAngles.x) != 0f || Mathf.Round(player.parent.eulerAngles.x) != 0f
                    || Mathf.Round(player.parent.eulerAngles.z) != 0f || Mathf.Round(camLock.localEulerAngles.z) != 0f || Mathf.Round(cumulPanYAxis) != 0f) {

                        //Resetting camera X angle
                        camLock.localEulerAngles = new Vector3(Mathf.LerpAngle(camLock.localEulerAngles.x, 0f, Time.deltaTime * lerpSpeed),
                            camLock.localEulerAngles.y, camLock.localEulerAngles.z);

                        //Resetting player X angle
                        player.parent.eulerAngles = new Vector3(Mathf.LerpAngle(player.parent.eulerAngles.x, 0f, Time.deltaTime * lerpSpeed),
                            player.parent.eulerAngles.y, player.parent.eulerAngles.z);

                        //Resetting player Z angle (Tilt)
                        player.parent.eulerAngles = new Vector3(player.parent.eulerAngles.x, player.parent.eulerAngles.y,
                            Mathf.LerpAngle(player.parent.eulerAngles.z, 0f, Time.deltaTime * lerpSpeed));

                        //Resetting camera Z angle (Tilt)
                        camLock.localEulerAngles = new Vector3(camLock.localEulerAngles.x, camLock.localEulerAngles.y,
                            Mathf.LerpAngle(camLock.localEulerAngles.z, 0f, Time.deltaTime * lerpSpeed));

                        //Resetting cumulative Y pan angle
                        cumulPanYAxis = Mathf.Lerp(cumulPanYAxis, 0f, Time.deltaTime * lerpSpeed);

                        return;

                    } else {

                        skydivingDone = true;

                    }

                } 

                cumulPanYAxis += panAxis.y;
                cumulPanYAxis = Mathf.Clamp(cumulPanYAxis, -camDeltaXAngleLimit, camDeltaXAngleLimit);

                camLock.localEulerAngles = new Vector3(cumulPanYAxis * cameraSensitivityY, 0f, 0f);
                player.parent.eulerAngles = new Vector3(0f, player.parent.eulerAngles.y, 0f);

            }
        } else {    //YPAN ACCELERATED

            /*
            //Adjusting skydiving beginning camera angle
            if (skydivingDone) {

                float down = 0.24f * cameraSensitivityY, rDown = Mathf.Round(down);
                if (Mathf.Round(camLock.localEulerAngles.x) != 0f 
                    || Mathf.Round(player.parent.eulerAngles.x) != rDown || Mathf.Round(cumulPanYAxis) != rDown) {

                    //Resetting camera X angle
                    camLock.localEulerAngles = new Vector3(Mathf.LerpAngle(camLock.localEulerAngles.x, 0f, Time.deltaTime * lerpSpeed),
                        camLock.localEulerAngles.y, camLock.localEulerAngles.z);

                    //Resetting player X angle
                    player.parent.eulerAngles = new Vector3(Mathf.LerpAngle(player.parent.eulerAngles.x, down, Time.deltaTime * lerpSpeed),
                        player.parent.eulerAngles.y, player.parent.eulerAngles.z);

                    //Resetting cumulative Y pan angle
                    cumulPanYAxis = Mathf.Lerp(cumulPanYAxis, down, Time.deltaTime * lerpSpeed);

                    return;

                } else {

                    skydivingDone = false;

                }

            }
            */

            if (skydivingDone)
                skydivingDone = false;

            //Check if Camera is Outside Accelerating Bound (Not Accel -> (-0.24, 0.24) | Accel -> (0, 0.24))
            if (cumulPanYAxis < 0f) {
                cumulPanYAxis = 0f;
            } else {
                cumulPanYAxis += panAxis.y;
                cumulPanYAxis = Mathf.Clamp(cumulPanYAxis, 0f, 0.24f);
            }

            //Resetting Rotations
            if (Mathf.Round(camLock.localEulerAngles.x) != 0f) {

                //Resetting camera X angle
                camLock.localEulerAngles = new Vector3(Mathf.LerpAngle(camLock.localEulerAngles.x, 0f, Time.deltaTime * lerpSpeed),
                    camLock.localEulerAngles.y, camLock.localEulerAngles.z);

                //Setting player X angle (Previous Transition)
                player.parent.eulerAngles = new Vector3(Mathf.LerpAngle(player.parent.eulerAngles.x, cumulPanYAxis * cameraSensitivityY, Time.deltaTime * lerpSpeed),
                    player.parent.eulerAngles.y, player.parent.eulerAngles.z);


                return;

            }
             
            
            if (deltaTilt * horizontalAxis < 0f)
                deltaTilt = Mathf.MoveTowards(deltaTilt, horizontalAxis * skydiveCameraTilt, Time.deltaTime * 300f);
            else
                deltaTilt = Mathf.MoveTowards(deltaTilt, horizontalAxis * skydiveCameraTilt, Time.deltaTime * 100f);

            player.parent.eulerAngles = new Vector3(cumulPanYAxis * cameraSensitivityY, player.parent.eulerAngles.y, -deltaTilt);
            camLock.localEulerAngles = new Vector3(0f, 0f, deltaTilt);

        }
                        
    }
    #endregion
    #region gravityEffect
    private void gravityEffect() {

        if (jumping || Physics.CheckBox(player.position, new Vector3(gravityCheckWidth, 0.1f, gravityCheckWidth), player.rotation,LayerMask.GetMask("Ground"))) {
            gravityAccel = 0f;
            fallDistance = 0f;
            return;
        }
        
        RaycastHit hit;
        Physics.Raycast(new Ray(player.position, Vector3.down), out hit);

        fallDistance = -hit.distance;

        if (gravityAccel < terminalGravityAccel)
            gravityAccel += gravityAccelerationIndex;

        charController.Move(new Vector3(0f, -(((!skydiving ? gravity : (minSkydivingGravity - ((minSkydivingGravity - maxSkydivingGravity) * maxAxis))) * gravityAccel * Time.deltaTime)), 0f));
        
        if (skydiving && maxAxis != 0f && ((!keyboardAndMouse && joystickTouch.position != Vector2.zero) || keyboardAndMouse)) {
            
            if (verticalAxis > 0)
                verticalAxis *= skydivingSpeedMultiplier;
            else
                verticalAxis = 0f;
            horizontalAxis *= skydivingSpeedMultiplier;
        }
        
    }
    #endregion
    #region jumpHandler
    private void jumpHandler() {

        if (jumping && jumpElapsedTime < jumpDuration) {
            charController.Move(Vector3.up * jumpHeight * Time.deltaTime);
            jumpElapsedTime += Time.deltaTime;
        } else if (jumpElapsedTime >= jumpDuration) {
            jumpElapsedTime = 0f;
            jumping = false;
        }

    }
    #endregion
    #region isAbleToStand
    private bool isAbleToStand(bool crouching) {

        RaycastHit hit;
        Ray ray = new Ray(player.position + new Vector3(0f, crouching ? crouchingColliderParam.y : standingColliderParam.y, 0f), Vector3.up);

        if(Physics.Raycast(ray, out hit, LayerMask.GetMask("Ground")) && hit.distance < standHeightLimit)
            return false;

        return true;

    }
    #endregion
    #region cameraCollisionOcclusionHandler
    private void cameraCollisionOcclusionHandler(bool crouching) {
        
        if (Physics.CheckCapsule(cam.position, camLock.position, cameraColliderRadius, LayerMask.GetMask("Ground"))) { 
            
            if (Vector3.Distance(cam.position, camLock.position) >= minCameraDistance)
                cam.position = Vector3.MoveTowards(cam.position, camLock.position, cameraAdjustmentSpeed * Time.deltaTime);

        } else if (!Physics.CheckSphere(cam.position, cameraProximityRadiusOffset, LayerMask.GetMask("Ground"))) {
            
            if (Vector3.Distance(cam.position, camLock.position) <= maxCameraDistance)
                cam.position = Vector3.MoveTowards(cam.position, camLock.position, -cameraAdjustmentSpeed * Time.deltaTime);

        }

    }
    #endregion

    #endregion

}
