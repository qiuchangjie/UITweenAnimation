using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct NameMapping
{
    public string PropertyName;
    public string CustomName;
    public string GetDisplayName()
    {
        return string.Format("{0}({1})", PropertyName, CustomName);
    }
}

[Serializable]
public class ShaderPropertyNameData
{
    public string ShaderName;
    public List<NameMapping> NameMappings;
}

public class ShaderPropertyNamesPreset : ScriptableObject
{
    public List<ShaderPropertyNameData> NameData;
    public string GetDisplayName(string shaderName, string propertyName)
    {
        if (null != NameData)
        {
            for (int i = 0; i < NameData.Count; ++i)
            {
                ShaderPropertyNameData data = NameData[i];
                if (data.ShaderName == shaderName
                    && null != data.NameMappings)
                {
                    for (int j = 0; j < data.NameMappings.Count; ++j)
                    {
                        if (data.NameMappings[j].PropertyName == propertyName)
                        {
                            return data.NameMappings[j].GetDisplayName();
                        }
                    }
                }
            }
        }
        return propertyName;
    }
}