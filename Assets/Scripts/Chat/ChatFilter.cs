using UnityEngine;
using System.Collections.Generic;

public class ChatFilter : MonoBehaviour
{
    [SerializeField] private string[] bannedWords;
    [SerializeField] private float spamThreshold = 3f; // Nachrichten pro Sekunde
    
    private Dictionary<string, Queue<float>> playerMessageTimes = new Dictionary<string, Queue<float>>();
    
    public bool IsMessageAllowed(string sender, string message)
    {
        // Spam-Check
        if (IsSpamming(sender))
        {
            return false;
        }
        
        // Profanity-Check
        if (ContainsBannedWords(message))
        {
            return false;
        }
        
        return true;
    }
    
    bool IsSpamming(string sender)
    {
        if (!playerMessageTimes.ContainsKey(sender))
        {
            playerMessageTimes[sender] = new Queue<float>();
        }
        
        var times = playerMessageTimes[sender];
        float currentTime = Time.time;
        
        // Remove old timestamps
        while (times.Count > 0 && currentTime - times.Peek() > 1f)
        {
            times.Dequeue();
        }
        
        // Add current timestamp
        times.Enqueue(currentTime);
        
        return times.Count > spamThreshold;
    }
    
    bool ContainsBannedWords(string message)
    {
        string lowerMessage = message.ToLower();
        foreach (string bannedWord in bannedWords)
        {
            if (lowerMessage.Contains(bannedWord.ToLower()))
            {
                return true;
            }
        }
        return false;
    }
}
