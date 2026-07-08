using System;
using System.Collections.Generic;

[Serializable]
public class LearnedWordsSave
{
    public int version = 1;
    public List<string> wordIDs = new List<string>();
}
