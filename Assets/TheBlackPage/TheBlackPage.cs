using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class TheBlackPage : MonoBehaviour {
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable[] Arrows;
    public KMSelectable PlayButton;
    public KMSelectable SubmitButton;

    public Light[] Lights;
    public Renderer[] LightBulbs;
    public Material[] BulbMaterials;
    public TextMesh[] Counters;

    public Renderer SheetMusic;
    public TextMesh BlackPageText;
    public Renderer BigScreen;
    public Material[] BigScreenMaterials;

    public Material[] MeasureMaterials;

    // Logging info
    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved = false;

    // Solving info
    private int start = 1;
    private int end = 30;

    private int leftValueIndex = 0;
    private int rightValueIndex = 29;

    private readonly string[] measureValues = { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15",
                                                "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30"};

    private bool startFirstHalf = true;
    private bool canSubmit = false;
    private bool pressedPlay = false;
    private bool isPlaying = false;

    private static bool canPlayIntro = true;

    private readonly int[] illegalStartpoints = { 7, 8, 10, 12, 22, 23, 25, 28, 29, 30};
    private readonly int[] illegalEndpoints = { 6, 7, 9, 11, 21, 22, 24, 31, 32};

    // Run as bomb loads
    public void Awake() {
        moduleId = moduleIdCounter++;

        // Delegation
        for (int i = 0; i < Arrows.Length; i++) {
            int j = i;
            Arrows[i].OnInteract += delegate () {
                ArrowPressed(j);
                return false;
            };
        }

        PlayButton.OnInteract += delegate () { PlayButtonPressed(); return false; };
        SubmitButton.OnInteract += delegate () { SubmitButtonPressed(); return false; };

        Module.OnActivate += OnActivate;
    }

    // Sets up the solution
    public void Start() {
        leftValueIndex = UnityEngine.Random.Range(0, 30);
        rightValueIndex = UnityEngine.Random.Range(0, 30);

        bool legal = false;

        while (legal == false) {
            legal = true;

            start = UnityEngine.Random.Range(1, 28);
            end = start + UnityEngine.Random.Range(3, 6);

            for (int i = 0; i < illegalStartpoints.Length; i++) {
                if (start == illegalStartpoints[i]) {
                    legal = false;
                    break;
                }
            }

            for (int i = 0; i < illegalEndpoints.Length && legal == true; i++) {
                if (end == illegalEndpoints[i]) {
                    legal = false;
                    break;
                }
            }
        }

        if (start >= 16)
            startFirstHalf = false;

        DisplayToScreens();

        Debug.LogFormat("[The Black Page #{0}] The module is playing measures {1} to {2}.", moduleId, start, end);

        ToggleSheetMusic(true);
    }

    // Bombs lights turn on
    public void OnActivate() {
        canSubmit = true;
        StartCoroutine(FadeSheetMusic());

        if (canPlayIntro == true) {
            canPlayIntro = false;
            Audio.PlaySoundAtTransform("BlackPage_Intro", transform);
        }
    }

    // Turns the sheet music off after the lights turn on
    public IEnumerator FadeSheetMusic() {
        yield return new WaitForSeconds(2.0f);
        ToggleSheetMusic(false);
        Lights[0].enabled = false;
        Lights[1].enabled = false;
        canPlayIntro = true;
    }


    // Arrow button is pressed
    public void ArrowPressed(int i) {
        Arrows[i].AddInteractionPunch(0.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, gameObject.transform);

        if (moduleSolved == false) {
            // Loop around the index if bounds are reached
            if (i == 0) {
                leftValueIndex--;
                if (leftValueIndex < 0) leftValueIndex = 29;
            }

            else if (i == 1) {
                leftValueIndex++;
                if (leftValueIndex > 29) leftValueIndex = 0;
            }

            else if (i == 2) {
                rightValueIndex--;
                if (rightValueIndex < 0) rightValueIndex = 29;
            }

            else {
                rightValueIndex++;
                if (rightValueIndex > 29) rightValueIndex = 0;
            }

            DisplayToScreens();
        }
    }

    // Play button is pressed
    public void PlayButtonPressed() {
        PlayButton.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gameObject.transform);

        // Turns the light on if this is the first time playing the track
        if (pressedPlay == false) {
            ToggleSheetMusic(false);

            if (startFirstHalf == true) {
                LightBulbs[0].material = BulbMaterials[1];
                Lights[0].enabled = true;
            }

            else {
                LightBulbs[1].material = BulbMaterials[1];
                Lights[1].enabled = true;
            }
        }

        if (isPlaying == false)
            StartCoroutine(PlayTrack());

        pressedPlay = true;
    }

    // Submit button is pressed
    public void SubmitButtonPressed() {
        SubmitButton.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gameObject.transform);
        if (canSubmit == true && moduleSolved == false) {

            // Solve
            if (leftValueIndex + 1 == start && rightValueIndex + 1 == end) {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, gameObject.transform);
                GetComponent<KMBombModule>().HandlePass();
                moduleSolved = true;
                Debug.LogFormat("[The Black Page #{0}] Module solved!", moduleId);
            }

            // Strike
            else {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[The Black Page #{0}] Strike! You submitted measures {1} to {2}.", moduleId, leftValueIndex + 1, rightValueIndex + 1);
                ToggleSheetMusic(true);
            }
        }
    }


    // Displays the number to the screen
    private void DisplayToScreens() {
        Counters[0].text = measureValues[leftValueIndex];
        Counters[1].text = measureValues[rightValueIndex];
    }

    // Toggles the sheet music being visible
    private void ToggleSheetMusic(bool state) {
        if (state == true) {
            BigScreen.material = BigScreenMaterials[1];
            BlackPageText.text = "";
            SheetMusic.enabled = true;
            SheetMusic.material = MeasureMaterials[start - 1];
        }

        else {
            BigScreen.material = BigScreenMaterials[0];
            BlackPageText.text = "The Black Page";
            SheetMusic.enabled = false;
        }
    }


    // Plays the track
    public IEnumerator PlayTrack() {
        isPlaying = true;
        yield return new WaitForSeconds(0.5f);

        for (int i = start; i <= end; i++) {
            PlayMeasure(i);
            yield return new WaitForSeconds(4.0f);
        }

        isPlaying = false;
    }

    // Plays the sound for that measure
    private void PlayMeasure(int measure) {
        switch (measure) {
        case 1: Audio.PlaySoundAtTransform("BlackPage_1", transform); break; // Play 1
        case 2: Audio.PlaySoundAtTransform("BlackPage_2", transform); break; // Play 2
        case 3: Audio.PlaySoundAtTransform("BlackPage_3", transform); break; // Play 3
        case 4: Audio.PlaySoundAtTransform("BlackPage_4", transform); break; // Play 4
        case 5: Audio.PlaySoundAtTransform("BlackPage_5", transform); break; // Play 5
        case 6: Audio.PlaySoundAtTransform("BlackPage_6-8", transform); break; // Play 6-8
        case 9: Audio.PlaySoundAtTransform("BlackPage_9-10", transform); break; // Play 9-10
        case 11: Audio.PlaySoundAtTransform("BlackPage_11-12", transform); break; // Play 11-12
        case 13: Audio.PlaySoundAtTransform("BlackPage_13", transform); break; // Play 13
        case 14: Audio.PlaySoundAtTransform("BlackPage_14", transform); break; // Play 14
        case 15: Audio.PlaySoundAtTransform("BlackPage_15", transform); break; // Play 15
        case 16: Audio.PlaySoundAtTransform("BlackPage_16", transform); break; // Play 16
        case 17: Audio.PlaySoundAtTransform("BlackPage_17", transform); break; // Play 17
        case 18: Audio.PlaySoundAtTransform("BlackPage_18", transform); break; // Play 18
        case 19: Audio.PlaySoundAtTransform("BlackPage_19", transform); break; // Play 19
        case 20: Audio.PlaySoundAtTransform("BlackPage_20", transform); break; // Play 20
        case 21: Audio.PlaySoundAtTransform("BlackPage_21-23", transform); break; // Play 21-23
        case 24: Audio.PlaySoundAtTransform("BlackPage_24-25", transform); break; // Play 24-25
        case 26: Audio.PlaySoundAtTransform("BlackPage_26", transform); break; // Play 26
        case 27: Audio.PlaySoundAtTransform("BlackPage_27", transform); break; // Play 27
        case 28: Audio.PlaySoundAtTransform("BlackPage_28", transform); break; // Play 28
        case 29: Audio.PlaySoundAtTransform("BlackPage_29", transform); break; // Play 29
        case 30: Audio.PlaySoundAtTransform("BlackPage_30", transform); break; // Play 30

        default: break; // Play nothing
        }
    }
}