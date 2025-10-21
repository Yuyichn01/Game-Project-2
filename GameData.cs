using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[Serializable]
public class GameData
{
    public int score;

    public int currentSceneIndex;

    public float[] position;

    public GameData(int score)
    {
        this.score = score;
        /*this.position = new float[3];
        this.position[0] = position.x;
        this.position[1] = position.y;
        this.position[2] = position.z;
        */
    }
}
