using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Security.Cryptography;

enum DialogueState {
    Paused = 0,
    Talking = 1,
    Finishing = 2,
    Finished = 3
}

public class DialogueManager : MonoBehaviour
{
    // PUBLIC
    public TextMeshProUGUI textbox;

    public string[] text_array;
    public float time_between_characters;
    public float time_after_dialogue;

    // PRIVATE
    private DialogueState dialogue_state = DialogueState.Finished;
    private string current_string;
    private int current_string_length;
    private int current_char_placed;
    private float timer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateDialogue(0);
        }

        UpdateDialogue();
    }

    void UpdateDialogue()
    {
        timer += Time.deltaTime;
        // TALKING FUNCTIONALITY
        if (dialogue_state == DialogueState.Talking)
        {
            // ADD CHARACTERS TO TEXTBOX
            while (timer >= time_between_characters)
            {
                AddNextCharacter();
                // FINISHING CHECK
                if (current_string_length == current_char_placed)
                {
                    timer = 0;
                    dialogue_state = DialogueState.Finishing;
                }
            }
        }
        // FINISHING FUNCTIONALITY
        else if (dialogue_state == DialogueState.Finishing)
        {
            // FINISH CHECK
            if (timer >= time_after_dialogue)
            {
                dialogue_state = DialogueState.Finished;
                textbox.text = "";
            }
        }
    }

    void AddNextCharacter()
    {
        // Get next char and add to textbox
        char next_char = current_string[current_char_placed];
        current_char_placed ++;
        textbox.text = textbox.text + next_char;
        // Only decrease timer if it was not a space
        if (next_char != ' ')
        {
            timer -= time_between_characters;
        }

    }

    // Function to init a dialogue sequence
    void GenerateDialogue(int text_index)
    {
        textbox.text = "";
        dialogue_state = DialogueState.Talking;
        timer = 0;
        current_char_placed = 0;
        current_string = text_array[text_index];
        current_string_length = current_string.Length;
    }
}
