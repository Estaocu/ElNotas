using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBridgeListen
{
    public void ProcessNote(notesEnum incomingNote);
}
