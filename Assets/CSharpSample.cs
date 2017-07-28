using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Google.Protobuf;

public class CSharpSample : MonoBehaviour
{
    private void Start()
    {
        var sample = new Proto3Sample()
        {
            Id = 1,
            Email = "bluexo@hotmail.com",
            Name = "Alvin"
        };

        // Serialize to bytes
        var bytes = sample.ToByteArray();

        // Serialize to stream
        var stream = new MemoryStream();
        sample.WriteTo(stream);

        //Deserialize from bytes
        sample = Proto3Sample.Parser.ParseFrom(bytes);

        //Deserialize from json string
        sample = Proto3Sample.Parser.ParseJson("");
    }
}
