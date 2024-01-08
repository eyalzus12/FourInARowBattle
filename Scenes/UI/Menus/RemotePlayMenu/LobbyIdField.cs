using Godot;
using System;
using System.Text;

namespace FourInARowBattle;

public partial class LobbyIdField : LineEdit
{
    public override void _Ready()
    {
        TextChanged += OnTextChanged;
    }

    public void OnTextChanged(string newText)
    {   
        //remove non-numeric from text
        StringBuilder removeBad = new();
        foreach(char c in newText)
            if('0' <= c && c <= '9')
                removeBad.Append(c);
        Text = removeBad.ToString();
    }
}
