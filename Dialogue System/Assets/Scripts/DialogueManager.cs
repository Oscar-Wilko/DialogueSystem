using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Security.Cryptography;
using UnityEngine.UI;
using Unity.VisualScripting;
using System;

enum DialogueState {
    Paused      = 0,
    Talking     = 1,
    Finishing   = 2,
    Finished    = 3
}

enum CodingMode
{
    None,
    GatherCode,
    GatherVariables
}

public enum AnimationStyle
{
    None,
    Wiggle,
    Sin_Wave,
    Shaking,
    Gravity
}

struct CharacterValues
{
    public CharacterValues(int _w, int _p)
    {
        width= _w;
        start_position= _p;
    }

    public int width { get; }
    public int start_position { get; }
}

public class DialogueManager : MonoBehaviour
{
    // PUBLIC
    public string[] text_array;

    public GameObject sprite_prefab;
    public GameObject dialogue_box;
    public Texture2D spritesheet;

    // PRIVATE
    private List<CharacterValues> spritesheet_values = new List<CharacterValues>();
    private char code_character = '¦';

    private DialogueState dialogue_state = DialogueState.Finished;
    private string current_string;
    private int current_string_length;
    private int current_char_placed;
    private float timer;
    private CodingMode code_mode;

    private List<GameObject> created_characters= new List<GameObject>();
    private int total_words_in_line;
    private List<GameObject> current_line= new List<GameObject>();
    private List<GameObject> current_word= new List<GameObject>();
    private int starting_char_offset;

    // Code variables
    private string code_name;
    private List<string> code_arguements = new List<string>();
    private string current_code_arguement;

    // Tracked character effects
    private AnimationStyle current_anim_style = AnimationStyle.None;
    private float current_scale_modifier = 1.0f;
    private float current_speed_modifier = 1.0f;
    private Color current_colour = new Color(1.0f,1.0f,1.0f,1.0f);
    private float max_scale_in_line = 1.0f;

    private Vector2 anchor_pos = Vector2.zero;          // Tracked anchor position

    // Time and offset values
    private float time_between_characters = 0.05f;      // Time for a character to be created after another
    private float time_after_dialogue = 2.0f;           // Time until dialogue disappears
    private int new_line_offset = -40;                  // Pixels for each line break / return
    private int new_character_offset = 2;               // Pixels after each character
    private int space_offset = 16;                      // Pixels after space character

    // Spritesheet + dialogue box values
    private const int sprite_width = 32;                // Width of character in pixels
    private const int sprite_height = 32;               // Height of character in pixels
    private const int sprite_sheet_width = 16;          // Number of characters wide
    private const int sprite_sheet_height = 5;          // Number of characters tall
    private const int total_characters = 68;            // Number of characters in spritesheet
    private const float box_padding = 20;               // Size of padding around dialogue box

    void Start()
    {
        GenerateSpritesheetValues();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            GenerateDialogue(0);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            GenerateDialogue(1);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            GenerateDialogue(2);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            GenerateDialogue(3);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            GenerateDialogue(4);
        }

        UpdateDialogue();
    }

    private void UpdateDialogue()
    {
        timer += Time.deltaTime;

        switch (dialogue_state)
        {
            case DialogueState.Finished:
                break;


            case DialogueState.Paused:
                break;


            case DialogueState.Talking:
                // ADD CHARACTERS TO TEXTBOX
                while (timer >= (time_between_characters/current_speed_modifier))
                {
                    CheckNextCharacter();
                    // FINISHING CHECK
                    if (current_string_length == current_char_placed)
                    {
                        timer = 0;
                        dialogue_state = DialogueState.Finishing;
                    }
                }
                break;


            case DialogueState.Finishing:
                // FINISH CHECK
                if (timer >= time_after_dialogue)
                {
                    dialogue_state = DialogueState.Finished;
                    foreach(GameObject obj in created_characters) obj.GetComponentInChildren<LetterScript>().EndOfLife();
                    created_characters.Clear();
                }
                break;  
        }
    }

    private void CheckNextCharacter()
    {
        // Get next char and add to textbox
        char next_char = current_string[current_char_placed];

        // Toggle Coding Mode Check
        if (next_char == code_character)
        {
            if (code_mode == CodingMode.None)
            {
                code_name = "";
                current_code_arguement = "";
                code_arguements.Clear();
                code_mode = CodingMode.GatherCode;
            }
            else
            {
                ExecuteCodes();
                code_mode = CodingMode.None;
            }
            current_char_placed++;
            return;
        }

        // Update Depending on Current Coding Mode
        switch (code_mode)
        {
            case CodingMode.None:
                AddNextCharacter(next_char);
                break;
            case CodingMode.GatherCode:
                if (next_char == '=')
                {
                    code_mode = CodingMode.GatherVariables;
                }
                else if (next_char != ' ')
                {
                    code_name = code_name + next_char;
                }
                break;
            case CodingMode.GatherVariables:
                if (next_char == ',')
                {
                    code_arguements.Add(current_code_arguement);
                    current_code_arguement = "";
                }
                else if (next_char != ' ')
                {
                    current_code_arguement = current_code_arguement + next_char;
                }
                break;
        }
        current_char_placed++;
    }

    // Create next character in the string
    private void AddNextCharacter(char next_char)
    {
        int index_val = GetCharIndex((int)next_char);

        if (index_val == -1) return;

        // Add gap for spaces
        if (next_char == ' ')
        {
            current_word.Clear();
            if (current_line.Count > 0)
            {
                total_words_in_line++;
                anchor_pos = new Vector2(anchor_pos.x + space_offset * current_scale_modifier, anchor_pos.y);
            }
        }
        // Otherwise make the sprite and alter variables around it
        else
        {
            // Create sprite using my assets
            GameObject created_sprite = Instantiate(sprite_prefab, Vector3.zero, Quaternion.identity);
            created_sprite.transform.SetParent(dialogue_box.transform);
            created_sprite.transform.localPosition = new Vector3(anchor_pos.x - spritesheet_values[index_val].start_position * current_scale_modifier, anchor_pos.y, 0);


            // Create temporary texture for specific character
            int texture_pos_x = (index_val % sprite_sheet_width) * sprite_width;
            int texture_pos_y = (sprite_sheet_height - 1 - (int)(index_val / sprite_sheet_width)) * sprite_height;
            Texture2D croppedTexture = new Texture2D(sprite_width, sprite_height);
            croppedTexture.filterMode = FilterMode.Point;
            Color[] pixels = spritesheet.GetPixels(texture_pos_x, texture_pos_y, sprite_width, sprite_height);


            // Apply pixels to texture and raw images
            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply();
            created_sprite.GetComponentInChildren<RawImage>().texture = croppedTexture;
            created_sprite.GetComponentInChildren<RawImage>().color = current_colour;
            created_sprite.GetComponentInChildren<LetterScript>().SetAnimationStyle(current_anim_style);
            created_sprite.transform.localScale = new Vector3(current_scale_modifier,current_scale_modifier,1.0f);


            // Add to total characters added
            created_characters.Add(created_sprite);
            current_word.Add(created_sprite);
            current_line.Add(created_sprite);
            if (current_word.Count == 1) starting_char_offset = spritesheet_values[index_val].start_position;


            // Update new max character if there is one
            if (current_scale_modifier > max_scale_in_line) max_scale_in_line = current_scale_modifier;
            // Update new anchor position
            anchor_pos = new Vector2(anchor_pos.x + (new_character_offset + spritesheet_values[index_val].width)*current_scale_modifier, anchor_pos.y);

            // Reaching the end of the dialogue box
            if (anchor_pos.x >= dialogue_box.GetComponent<RectTransform>().rect.width - box_padding)
            {
                anchor_pos = new Vector2(box_padding, anchor_pos.y + new_line_offset * max_scale_in_line);
                max_scale_in_line = 0.0f;
                current_line.Clear();
                // Move all letters of last word down to next line (only if there are other words in that line)
                if (total_words_in_line > 0)
                {
                    if (next_char != ' ') current_line.Add(created_sprite);
                    // Find min and max letter of word
                    float lowest_x = -1.0f;
                    float highest_x = -1.0f;
                    foreach (GameObject letter in current_word)
                    {
                        if (lowest_x == -1 || letter.transform.localPosition.x < lowest_x) lowest_x = letter.transform.localPosition.x;
                    }
                    // Shift all letters down by the min x
                    foreach (GameObject letter in current_word)
                    {
                        letter.transform.localPosition = new Vector3(letter.transform.localPosition.x - lowest_x + box_padding - starting_char_offset * current_scale_modifier, anchor_pos.y, 0.0f);
                    }
                    // Update anchor pos with width of word
                    foreach (GameObject letter in current_word)
                    {
                        if (highest_x == -1 || letter.transform.localPosition.x > highest_x) highest_x = letter.transform.localPosition.x;
                    }
                    anchor_pos = new Vector2(highest_x + (new_character_offset + spritesheet_values[index_val].width + spritesheet_values[index_val].start_position) * current_scale_modifier, anchor_pos.y);
                    total_words_in_line = 0;
                }
            }

            // Decrease timer
            timer -= time_between_characters/current_speed_modifier;
        }
    }

    // With all stored code_names and code_variables, execute them
    private void ExecuteCodes()
    {
        if (current_code_arguement != "") code_arguements.Add(current_code_arguement);

        switch (code_name)
        {
            /// COMMANDS
            // New Line
            case "n":
            case "new":
                anchor_pos = new Vector2(box_padding, - box_padding + anchor_pos.y + new_line_offset);
                max_scale_in_line = 0.0f;
                break;

            /// TEXT CHANGES
            // Colour Change
            case "c":
            case "col":
            case "color":
                List<float> args = new List<float>();
                for (int i = 0; i < 4; i ++)
                {
                    args.Add(i >= code_arguements.Count ? 1.0f : (float)Convert.ToDouble(code_arguements[i]));
                }
                current_colour = new Color(args[0], args[1], args[2], args[3]);
                break;
            // Size
            case "si":
            case "size":
                if (code_arguements.Count >= 0) current_scale_modifier = (float)Convert.ToDouble(code_arguements[0]);
                break;
            // Speed
            case "sp":
            case "speed":
                if (code_arguements.Count >= 0) current_speed_modifier = (float)Convert.ToDouble(code_arguements[0]);
                break;

            /// ANIMATION STYLES
            case "a":
            case "an":
            case "anim":
            case "animation":
                if (code_arguements.Count >= 0)
                {
                    switch (code_arguements[0]) 
                    {
                        // Gravitied
                        case "g":
                        case "grav":
                            current_anim_style = AnimationStyle.Gravity;
                            break;
                        // Wiggle Effect
                        case "w":
                        case "wiggle":
                            current_anim_style = AnimationStyle.Wiggle;
                            break;
                        // Sin Wave Effect
                        case "si":
                        case "sin":
                        case "sine":
                            current_anim_style = AnimationStyle.Sin_Wave;
                            break;
                        // Shake
                        case "sh":
                        case "shake":
                            current_anim_style = AnimationStyle.Shaking;
                            break;
                    }
                }
                break;
        }
    }

    // Return the index of the character given based on where it is on the spritesheet
    // -1 return means a false return
    private int GetCharIndex(int ascii_val)
    {
        int index = ascii_val;

        // If above max index then return false int
        if (index > 126) return -1;
        // Shift { -> ~ down by 59
        else if (index > 122) index -= 59;
        // Shift lower case a -> z down by 64
        else if (index > 96) index -= 64;
        // Shift ! -> _ down by 32
        else if (index > 31 && index != 96) index -= 32;
        // If below max index then return false int
        else return -1;

        return index;
    }

    // Function to init a dialogue sequence
    private void GenerateDialogue(int text_index)
    {
        // Reset Textbox and Variables
        ResetVariables();

        dialogue_state = DialogueState.Talking;
        current_string = text_array[text_index];
        current_string_length = current_string.Length;
    }

    private void ResetVariables()
    {
        // Lists
        foreach (GameObject obj in created_characters) obj.GetComponentInChildren<LetterScript>().EndOfLife();
        created_characters.Clear();
        current_line.Clear();
        current_word.Clear();

        // Trackers
        timer = 0;
        current_char_placed = 0;
        anchor_pos = new Vector2(box_padding,-box_padding);
        max_scale_in_line = 0.0f;
        total_words_in_line = 0;
        
        // Code variables
        code_mode = CodingMode.None;
        current_anim_style = AnimationStyle.None;
        current_scale_modifier = 1.0f;
        current_colour = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        current_speed_modifier = 1.0f;
    }


    // Generate each characters widths and positions based on pixel values
    private void GenerateSpritesheetValues()
    {
        int texture_pos_x;
        int texture_pos_y;
        for (int i = 0; i < total_characters; i ++)
        {
            texture_pos_x = (i % sprite_sheet_width) * sprite_width;
            texture_pos_y = (sprite_sheet_height - 1 - (int)(i / sprite_sheet_width)) * sprite_height;
            var pixels = spritesheet.GetPixels(texture_pos_x, texture_pos_y, sprite_width, sprite_height);
            spritesheet_values.Add(CharacterValueGeneration(pixels));
        }
    }

    // Generate value for single character
    private CharacterValues CharacterValueGeneration(Color[] pixels)
    {
        int lowest_x = 0; 
        int highest_x = sprite_width-1;

        for (int x = 0; x < sprite_width; x ++)
        {
            for (int y = 0; y < sprite_height; y ++)
            {
                if (pixels[x + y * sprite_width].a != 0)
                {
                    lowest_x = x; break;
                }
            }
            if (lowest_x != 0) break;
        }

        for (int x = sprite_width-1; x >= 0; x --)
        {
            for (int y = 0; y < sprite_height; y ++)
            {
                if (pixels[x + y * sprite_width].a != 0)
                {
                    highest_x = x; break;
                }
            }
            if (highest_x != sprite_width-1) break;
        }
        return new CharacterValues(highest_x - lowest_x + 1,lowest_x);
    }
}
