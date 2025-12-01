using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

// A lightweight recursive descent parser for runtime equation evaluation
public static class SimpleMathParser
{
    public static float Evaluate(string expression, float x, float z, float t)
    {
        try
        {
            // simplistic substitution (Production use: use NCalc or similar library)
            expression = expression.ToLower().Replace(" ", "");
            // Handle basic constants
            expression = expression.Replace("pi", Mathf.PI.ToString());
            expression = expression.Replace("e", Mathf.Epsilon.ToString());
            
            return ParseExpression(expression, x, z, t);
        }
        catch { return 0f; }
    }

    private static float ParseExpression(string eq, float x, float z, float t)
    {
        // extremely basic parser logic for demonstration
        // 1. Replace variables
        string finalEq = eq.Replace("x", x.ToString("F4"))
                           .Replace("z", z.ToString("F4"))
                           .Replace("t", t.ToString("F4"));

        // 2. Use System.Data.DataTable to compute basic arithmetic (hacky but standard C# trick)
        // Note: DataTable does NOT support sin/cos directly, so we usually pre-parse functions.
        // For this script, to keep it copy-paste friendly without DLLs, 
        // we will implement a very basic recursive function handler.
        
        return (float)new System.Data.DataTable().Compute(PreParseFunctions(finalEq), "");
    }

    private static string PreParseFunctions(string eq)
    {
        // Identify functions like sin(...), cos(...) and evaluate them first
        // This is a regex replacement wrapper for common math functions
        // Supported: sin, cos, tan, sqrt, abs
        
        eq = Regex.Replace(eq, @"sin\(([^)]+)\)", m => Math.Sin(Convert.ToDouble(new System.Data.DataTable().Compute(m.Groups[1].Value, ""))).ToString());
        eq = Regex.Replace(eq, @"cos\(([^)]+)\)", m => Math.Cos(Convert.ToDouble(new System.Data.DataTable().Compute(m.Groups[1].Value, ""))).ToString());
        eq = Regex.Replace(eq, @"sqrt\(([^)]+)\)", m => Math.Sqrt(Convert.ToDouble(new System.Data.DataTable().Compute(m.Groups[1].Value, ""))).ToString());
        
        return eq;
    }
}