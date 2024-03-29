using Godot;
using System;
using System.Text;

namespace FourInARowBattle;

/// <summary>
/// This class is an editable field that only allows numbers inside
/// </summary>
public partial class LobbyIdField : LineEdit
{
    public override void _Ready()
    {
        TextChanged += OnTextChanged;
    }

    private void OnTextChanged(string newText)
    {
        ArgumentNullException.ThrowIfNull(newText);
        int caretIndex = CaretColumn;
        //remove non-numeric from text
        StringBuilder removeBad = new();
        for(int i = 0; i < newText.Length; ++i)
        {
            char c = newText[i];
            if('0' <= c && c <= '9')
            {
                removeBad.Append(c);
            }
            else
            {
                //we are removing a character before the caret.
                if(i < caretIndex)
                {
                    caretIndex--;
                }
            }
        }
        Text = removeBad.ToString();
        CaretColumn = caretIndex; //update caret position
    }
}
