using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class CommandDecryptor
{
    #region DATAS
    public struct TextInfos
    {
        public CommandAction commandAction;
        public double time;
        public TimeInfo timeInfo;

    }
    public enum CommandAction
    {
        Jump, Move_Fwd, Move_Back, Move_Left, Move_Right
    }
    public enum TimeInfo
    {
        Second, Minutes, Hours, Days
    }

    /// <summary>
    /// Dictionary to get time multiplicator to convert the time asked in second
    /// </summary>
    private static readonly Dictionary<TimeInfo, double> timeConverterLibrary = new Dictionary<TimeInfo, double>()
    {
        {TimeInfo.Second,  1 },
        {TimeInfo.Minutes, 60 },
        {TimeInfo.Hours, 3600},
        {TimeInfo.Days, 86400 },
    };

    /// <summary>
    /// Use to decrypt the time
    /// </summary>
    private static readonly Dictionary<TimeInfo, string[]> timeDecryptorInfo = new Dictionary<TimeInfo, string[]>()
    {
        {TimeInfo.Second,  new string[]{ "s", "sec", "second"} },
        {TimeInfo.Minutes,  new string[]{ "m", "min", "minute"} },
        {TimeInfo.Hours,  new string[]{"h", "heure", "hour"} },
        {TimeInfo.Days,  new string[]{"d", "jour", "day"} },
    };

    /// <summary>
    /// Use to decrypt the text
    /// </summary>
    private static readonly Dictionary<CommandAction, string[]> commandDecryptorInfos = new Dictionary<CommandAction, string[]>()
    {
        {CommandAction.Jump,  new string[]{"jump", "saut" } },
        {CommandAction.Move_Fwd,  new string[]{ "avance", "marche", "walk", "avant"} },
        {CommandAction.Move_Back,  new string[]{ "recule", "arrière", "backward", "back"} },
        {CommandAction.Move_Left,  new string[]{ "gauche", "left"} },
        {CommandAction.Move_Right,  new string[]{ "droite", "right"} }
    };

    #endregion


    public static event Action<TextInfos> textDecryptEvent;
    public static event Action<bool> onCommandIsWriting;

    public static void CallOnCommandIsWritingEvent(bool pState) => onCommandIsWriting?.Invoke(pState);

    /// <summary>
    /// Use to find the a "double" value in a text, principally to find the time
    /// </summary>
    /// <param name="pText"></param>
    /// <param name="pStartIndex"> The default start index is 0</param>
    /// <param name="pEndIndex"> The default end index is 1</param>
    /// <returns> A "double" to get the entire value and not an rounded value </returns>
    private static double FindDoubleValueInText(ReadOnlySpan<char> pText, int pStartIndex = 0, int pEndIndex = 1)
    {
        double lValue = -1;

        // Check if the text isn't empty
        if (pStartIndex >= pText.Length-1) return lValue;

        double lDouble, lDouble2;
        bool lFloatIsFounded = false;
        StringBuilder lText = new StringBuilder();

        pText = pText.ToString().Replace(".", ",");

        // Use to through the text to get a number
        for (int i = pStartIndex; i <= pEndIndex; i++)
        {
            if (double.TryParse(pText.Slice(i, 1), out lDouble))
            {
                lText.Append(pText[i]);

                // If the number was found, through the text another time to get the entire value  
                for (int j = i + 1; j < pEndIndex; j++)
                {
                    // Use to get a floating value
                    if (!lFloatIsFounded && pText[j] == ',')
                    {
                        lText.Append(pText[j]);
                        lFloatIsFounded = true;
                    }
                    else
                    {
                        if (double.TryParse(pText.Slice(j, 1), out lDouble2))
                            lText.Append(pText[j]);
                        else
                            goto ReturnValue;
                    }
                }
            }
        }

        ReturnValue:
        if (double.TryParse(lText.ToString(), out lValue))
        {
            return lValue;
        }
        else return -1;
    }

    private static double ConvertTime(ReadOnlySpan<char> pText, double pTime)
    {
        TimeInfo lTime = TimeInfo.Second;
        foreach (var lItems in timeDecryptorInfo)
        {
            foreach (var lItem in lItems.Value)
            {
                if (pText.Contains(pTime + lItem, StringComparison.CurrentCulture) || 
                    pText.Contains(pTime + " " + lItem, StringComparison.CurrentCulture))
                {
                    lTime = lItems.Key;
                    goto Convert;
                }
            }
        }
        Convert:
        return pTime * timeConverterLibrary[lTime];
    }

    public static void DecryptText(string pText)
    {
        pText = pText.ToLower();
        TextInfos lTI;
        List<TextInfos> lCommands = new List<TextInfos>();

        foreach (KeyValuePair<CommandAction, string[]> lItem in commandDecryptorInfos)
        {
            foreach (string lValue in lItem.Value)
            {
                if (pText.Contains(lValue)) 
                {
                    lTI = new TextInfos();
                    lTI.commandAction = lItem.Key;

                    // TIME
                    // Check a first time after the command
                    lTI.time = FindDoubleValueInText(pText, pText.IndexOf(lValue), pText.Length-1);

                    // Check a second time before the command if the time wasn't found
                    if (lTI.time == -1) lTI.time = FindDoubleValueInText(pText, 0, pText.IndexOf(lValue));

                    lTI.time = lTI.time == - 1 ? 0 : ConvertTime(pText, lTI.time);
                    textDecryptEvent.Invoke(lTI);
                    return;
                }
            }
        }
        Debug.Log("Command Fail !");
    }
}