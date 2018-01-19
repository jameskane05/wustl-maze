using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MazeController : MonoBehaviour {

    public Text text;
    public GameObject player;
    public GameObject arrows;
    public GameObject startHall;
    public GameObject endHall;
    public GameObject overheadCam;
    public Transform startLoc;
    public Transform endLoc;
    static public bool hasEnded;
    public bool hasStarted;
    static public Material pathColor;
    static public int onCorner = 0;
    static public float totalTime;
    static public bool cornerEvent = false;
	static bool grabAndFaceForward;
    static bool rotateThreeSixty = false;
    static bool checkForFinishedRot = false;
    public Transform[] Rotations;
	public int pauseDuration;
	public float rotationDuration;
    static bool cornerActive;
    public Sprite[] introImgs;
    public static GameObject subjectInstance;
    public static UnityStandardAssets.Characters.FirstPerson.FirstPersonController controller;
    private Vector3 lastPos;
    private Vector3 currentPos;
    private int picCounter;
    private bool startCorner = false;
    static private float totalDistance;
    static private float avgVelocity;
    static private List<string> path;
    static public Transform cornerTransform;
	public GameObject[] landmarks;
	static public ExperimentSettings _expInstance;
	private List<string> experimentInfo;
	public int durationPerPic;
    
	void Start() {
		_expInstance = ExperimentSettings.GetInstance ();
		InitMaze ();
        totalDistance = 0;
        totalTime = 0;
        path = new List<string>();
        hasEnded = false;
        hasStarted = false;
        picCounter = 0;
		grabAndFaceForward = false;
		subjectInstance = GameObject.FindWithTag ("Player");
		controller = subjectInstance.GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>();
		controller.enabled = false;
		lastPos = subjectInstance.transform.position;
		currentPos = subjectInstance.transform.position;
		GameObject _goTransform = new GameObject ();
		cornerTransform = _goTransform.transform;
    }

    void Update()
    { 
        if (Input.GetKeyDown(KeyCode.E) && !hasEnded)
        {
            MazeEnd();
            SceneManager.LoadScene(0);
        }

        else if (Input.GetKeyDown(KeyCode.E) && hasEnded)
            SceneManager.LoadScene(0);

        if (Input.GetKeyDown("space") && !hasStarted)
		{
            if (_expInstance.MazeSettings.MultiIntro) StartCoroutine(MultiPicIntro());
			else if (_expInstance.MazeSettings.SingleIntro)
                StartCoroutine(SinglePicIntro());
            else {
                StartCoroutine(WaitAndStart());
                InvokeRepeating("TrackPathEverySecond", 2.0f, 1.0f);
                controller.enabled = true;
            }
        }

        // Trial has begun, do the following:
        if (hasStarted && !hasEnded) {
            currentPos = subjectInstance.transform.position;
            totalDistance += Vector3.Distance(currentPos, lastPos);
            totalTime += Time.deltaTime;
            lastPos = currentPos;
        }
    }

    private void LateUpdate()
    {
		if (_expInstance.MazeSettings.Pause || _expInstance.MazeSettings.Rotate) {
			if (cornerEvent) {
				grabAndFaceForward = true;
				cornerEvent = false;
			}

			if (grabAndFaceForward) GrabAndFaceForward ();
			if (rotateThreeSixty) RotateThreeSixty ();

			if (controller.isActiveAndEnabled)
				if (Input.anyKey || Input.GetAxis("Horizontal1") > 0 || Input.GetAxis("Horizontal1") < 0) text.text = "";  // reset "Please continue" text on player input
		}
    }

	public void InitMaze() {

		if (_expInstance.MazeSettings.MazeName == MazeNameEnum.JP) {
			Instantiate (player, startLoc.transform);
		}

		else if (_expInstance.MazeSettings.MazeName == MazeNameEnum.VE) {
			Instantiate (player, startLoc.transform);
			endHall.AddComponent<End> ();
			text.text = "Please navigate to the end of the hall as quickly as possible\r\n\r\n Hit the spacebar to begin.";
		}

		// Load AP, SPT, TP conditions
		else if (_expInstance.ExperimentType == ExperimentTypeEnum.AP || 
			_expInstance.ExperimentType == ExperimentTypeEnum.SPT ||
			_expInstance.ExperimentType == ExperimentTypeEnum.TP ) {

			if (_expInstance.MazeSettings.Arrows) {
				text.text = "Please follow the arrows.\r\n\r\nHit the spacebar to begin.";
				arrows.SetActive (true);
			} 
			else if (!_expInstance.MazeSettings.Arrows
				&& _expInstance.MazeSettings.MazeName != MazeNameEnum.JP
				&& _expInstance.MazeSettings.MazeName != MazeNameEnum.VE)
			{
				text.text = "Please recreate the route.\r\n\r\nHit the spacebar to begin.";
				arrows.SetActive (false);
			}

			if (_expInstance.ExperimentType == ExperimentTypeEnum.TP &&
				(_expInstance.MazeSettings.SingleIntro || _expInstance.MazeSettings.MultiIntro)) {
				text.text = "Please try to remember the order and turn direction.\r\n\r\nHit the spacebar to begin.";
			}

			if (_expInstance.MazeSettings.Reverse) {
				Instantiate (player, endLoc.transform);
				startHall.AddComponent<End> ();
			} else {
				Instantiate (player, startLoc.transform);
				if (_expInstance.MazeSettings.MazeName != MazeNameEnum.JP)
					endHall.AddComponent<End> (); 
			}
		}
			
		// Load CM conditions
		if (_expInstance.ExperimentType == ExperimentTypeEnum.CM) {

			if (_expInstance.MazeSettings.MazeName == MazeNameEnum.TP) {
				Instantiate (player, startLoc.transform);
				Debug.Log (_expInstance.MazeSettings.LandmarkGoalCM);
				if (!string.IsNullOrEmpty(_expInstance.MazeSettings.LandmarkGoalCM)) {
					landmarks [0].AddComponent<End> ();
					text.text = "Please locate the landmark.\r\n\r\nHit the spacebar to begin.";
				} else {
					text.text = "Please explore the environment.\r\n\r\nHit the spacebar to begin.";
				}
			} 

			else if (_expInstance.MazeSettings.MazeName == MazeNameEnum.CM) {
				Debug.Log (_expInstance.MazeSettings.StartLocationCM);
				GameObject startLoc = GameObject.Find (_expInstance.MazeSettings.StartLocationCM);
				Debug.Log (startLoc);
				Instantiate (player, startLoc.transform);
				if (!string.IsNullOrEmpty (_expInstance.MazeSettings.LandmarkGoalCM)) {
					string landmarkName = _expInstance.MazeSettings.LandmarkGoalCM;
					GameObject landmark = GameObject.Find (landmarkName);
					GameObject colliderObj = landmark.transform.GetChild (0).gameObject;
					colliderObj.AddComponent<End> ();
					text.text = "Please locate the landmark.\r\n\r\nHit the spacebar to begin.";
				} else {
					text.text = "Please explore the environment.\r\n\r\nHit the spacebar to begin.";
				}
			}
		}
	}
		
    public void GrabAndFaceForward()
    {
        controller.enabled = false;

        subjectInstance.transform.position = Vector3.MoveTowards(
            subjectInstance.transform.position,
			new Vector3(cornerTransform.position.x, cornerTransform.position.y + 1, cornerTransform.position.z),
            Time.deltaTime / 3
        );

        Rotations[onCorner].rotation = Quaternion.Euler(Rotations[onCorner].rotation.eulerAngles);

        subjectInstance.transform.rotation = Quaternion.Lerp(
            subjectInstance.transform.rotation,
            Rotations[onCorner].rotation,
            Time.deltaTime / 2
        );

		if (Mathf.Abs(Vector3.Distance(subjectInstance.transform.rotation.eulerAngles, Rotations[onCorner].rotation.eulerAngles)) <= 2f)
        {
            grabAndFaceForward = false;
			if (_expInstance.MazeSettings.Rotate)
				rotateThreeSixty = true;
			else if (_expInstance.MazeSettings.Pause) {
				Debug.Log ("Pause");
				StartCoroutine (PauseWithOptions (pauseDuration, enableCtrlAfter: true, grabAfter: false, textAfter: "Please continue."));
			}
        }
    }

    public void RotateThreeSixty()
    {
        controller.enabled = false;
		subjectInstance.transform.RotateAround(subjectInstance.transform.position, Vector3.up, 360 * Time.smoothDeltaTime / rotationDuration);

        if (Mathf.RoundToInt(Mathf.Abs(subjectInstance.transform.rotation.eulerAngles.y - Rotations[onCorner].rotation.eulerAngles.y)) > 5f)
        {
            checkForFinishedRot = true;
        }
        if (checkForFinishedRot)
        {
            if (Mathf.Abs(subjectInstance.transform.rotation.eulerAngles.y - Rotations[onCorner].rotation.eulerAngles.y) < 1f)
            {
                rotateThreeSixty = false;
                controller.enabled = true;
                checkForFinishedRot = false;
            }
        }
    }

    IEnumerator WaitAndStart()
    {
        text.text = "+";
        yield return new WaitForSeconds(1);
        Image img = GameObject.Find("Panel").GetComponent<Image>();
        img.color = UnityEngine.Color.clear;
        hasStarted = true;
        text.text = "";
		controller.enabled = true;
    }

    IEnumerator WaitToCheckDistance() {
        yield return new WaitForSeconds(2f);

    }

    public void TrackPathEverySecond()
    {
        int second = Mathf.RoundToInt(totalTime);
        path.Add( second.ToString() + ": " + subjectInstance.transform.position);
    }

	IEnumerator PauseWithOptions(int seconds, bool enableCtrlAfter = true, bool grabAfter = false, bool rotateAfter = false, string textAfter = "")
    {
        controller.enabled = false;
        yield return new WaitForSeconds(seconds);
        controller.enabled = enableCtrlAfter;
        grabAndFaceForward = grabAfter;
        rotateThreeSixty = rotateAfter;
		text.text = textAfter;
    }

    IEnumerator SinglePicIntro()
    {
        text.text = "";
        Image img = GameObject.Find("Panel").GetComponent<Image>();
        img.color = UnityEngine.Color.white;
		if (_expInstance.MazeSettings.MazeName == MazeNameEnum.TP) picCounter = 2;
		else picCounter = 8; // magic number for montage pic
        img.sprite = introImgs[picCounter];
		yield return new WaitForSeconds(introImgs.Length * durationPerPic);
        img.sprite = null;
        img.color = UnityEngine.Color.black;
        StartCoroutine(WaitAndStart());
    }

    IEnumerator MultiPicIntro()
    {
        text.text = "";
        Image img = GameObject.Find("Panel").GetComponent<Image>();
		while (picCounter <= introImgs.Length - 2)
        {
            img.color = UnityEngine.Color.white;
            img.sprite = introImgs[picCounter];
            yield return new WaitForSeconds(durationPerPic);
            picCounter++;
        }
        img.color = UnityEngine.Color.black;
        StartCoroutine(WaitAndStart());
    }
		
    static public void MazeEnd()
    {
        hasEnded = true;
        controller.enabled = false;
        Text text = GameObject.Find("Text").GetComponent<Text>();
        text.text = "You have reached the end.";
        TrailRenderer trail = GameObject.Find("Trail").GetComponent<TrailRenderer>();
        Material mat = Resources.Load("Yellow") as Material;
        trail.material = mat;
		avgVelocity = totalDistance / totalTime;
		List<string> experimentInfo = GetExperimentInfo ();

		if (!File.Exists (_expInstance.FileName))
			System.IO.File.WriteAllLines (_expInstance.FileName, experimentInfo.ToArray ());
		else
			foreach (string line in experimentInfo)
				System.IO.File.AppendAllText (_expInstance.FileName, line + "\r\n");
		System.IO.File.AppendAllText (_expInstance.FileName, "\r\n");
		foreach (string line in path)
			System.IO.File.AppendAllText (_expInstance.FileName, line +  "\r\n");
		System.IO.File.AppendAllText (_expInstance.FileName, "\r\n");
        TakePhoto();
		_expInstance.MazeSettings = new MazeSettings(); // hoping this line resets class defaults

		// It doesn't, can reset manually - but there's got to be a way to do that
		/*_expInstance.MazeSettings.Arrows = false;
		_expInstance.MazeSettings.Rotate = false;
		_expInstance.MazeSettings.Pause = false;
		_expInstance.MazeSettings.Reverse = false;
		_expInstance.MazeSettings.SingleIntro = false;
		_expInstance.MazeSettings.MultiIntro = false;*/
		
    }

	static private List<string> GetExperimentInfo () {
		List<string> experimentInfo = new List<string>();
		ExperimentSettings _expInstance = ExperimentSettings.GetInstance ();
		experimentInfo.Add ("Participant ID: " + _expInstance.ParticipantID);
		experimentInfo.Add ("Experimenter Initials: " + _expInstance.ExperimenterInitials);
		experimentInfo.Add ("Date: " + _expInstance.Date);
		experimentInfo.Add ("Experiment Type: " + _expInstance.ExperimentType);
		experimentInfo.Add ("Maze: " + _expInstance.MazeSettings.MazeName.ToString());

		if (_expInstance.ExperimentType == ExperimentTypeEnum.AP ||
			_expInstance.ExperimentType == ExperimentTypeEnum.SPT )
		{
			experimentInfo.Add ("Arrows: " + _expInstance.MazeSettings.Arrows.ToString ());
			experimentInfo.Add ("Reverse: " + _expInstance.MazeSettings.Reverse.ToString ());
		}

		if (_expInstance.ExperimentType == ExperimentTypeEnum.TP ){
			if (_expInstance.MazeSettings.SingleIntro)
				experimentInfo.Add ("Intro: " + _expInstance.MazeSettings.SingleIntro);
			else if (_expInstance.MazeSettings.MultiIntro)
				experimentInfo.Add ("Intro: " + _expInstance.MazeSettings.MultiIntro);
		}

		if (_expInstance.ExperimentType == ExperimentTypeEnum.CM) {
			experimentInfo.Add("Start Location: " + _expInstance.MazeSettings.StartLocationCM);
			if (!string.IsNullOrEmpty(_expInstance.MazeSettings.StartLocationCM))
				experimentInfo.Add ("Landmark:" + _expInstance.MazeSettings.LandmarkGoalCM);
		}

		experimentInfo.Add ("Distance: " + totalDistance);
		experimentInfo.Add ("Time: " + totalTime);
		experimentInfo.Add ("Avg. Velocity: " + avgVelocity);
		return experimentInfo;
	}

	static private void TakePhoto()
    {
		ExperimentSettings _expInstance = ExperimentSettings.GetInstance ();
        Camera cam = GameObject.Find("Overhead Cam").GetComponent<Camera>();
        RenderTexture currentRT = RenderTexture.active;
        var rTex = new RenderTexture(cam.pixelHeight, cam.pixelHeight, 16);
        cam.targetTexture = rTex;
        RenderTexture.active = cam.targetTexture;
        cam.Render();
        Texture2D tex = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        tex.Apply(false);
        RenderTexture.active = currentRT;
        byte[] bytes = tex.EncodeToPNG();
        Destroy(tex);
		string imageFileName = NameImageFile ();
        System.IO.File.WriteAllBytes(imageFileName + "-path.png", bytes);
    }

	static private string NameImageFile ()
	{
		ExperimentSettings _expInstance = ExperimentSettings.GetInstance ();
		string imageFileName = _expInstance.FileDir + "\\";
		imageFileName += _expInstance.MazeSettings.MazeName.ToString () + "-";

		if (_expInstance.ExperimentType == ExperimentTypeEnum.AP ||
			_expInstance.ExperimentType == ExperimentTypeEnum.SPT )
		{
			if (_expInstance.MazeSettings.Arrows) imageFileName += "Arrows-";
			if (_expInstance.MazeSettings.Reverse) imageFileName += "Rev-";
			else imageFileName += "Fwd-";
			if (_expInstance.MazeSettings.Rotate) imageFileName += "Rot-";
			else if (_expInstance.MazeSettings.Pause) imageFileName += "Pause-";
		}

		if (_expInstance.ExperimentType == ExperimentTypeEnum.TP) {
			if (_expInstance.MazeSettings.SingleIntro) imageFileName += "SingleIntro-";
			else if (_expInstance.MazeSettings.MultiIntro) imageFileName += "MultiIntro-";
		}
			
		if (_expInstance.ExperimentType == ExperimentTypeEnum.CM) {
			imageFileName += "Start" + _expInstance.MazeSettings.StartLocationCM + "-";
			if (!string.IsNullOrEmpty(_expInstance.MazeSettings.LandmarkGoalCM)) 
				imageFileName += "Landmark" + _expInstance.MazeSettings.LandmarkGoalCM;
		}

		imageFileName += "-" + DateTime.Now.ToString ("yyyyMMddHHmmss");
		imageFileName += ".txt";

		return imageFileName;
	}
}