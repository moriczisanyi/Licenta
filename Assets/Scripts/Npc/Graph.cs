using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
// A directed graph using
// adjacency list representation
public class Graph
{

    private int v;
    string path = Application.streamingAssetsPath + "/test.txt";
    private List<int>[] adjList;
    public Graph(int vertices)
    {
        this.v = vertices;
        initAdjList();
    }

    private void initAdjList()
    {
        adjList = new List<int>[v];

        for (int i = 0; i < v; i++)
        {
            adjList[i] = new List<int>();
        }
    }

    public void addEdge(int u, int v)
    {
        adjList[u].Add(v);
    }

    public void printAllPaths(int s, int d)
    {
        File.WriteAllText(path, String.Empty);
        bool[] isVisited = new bool[v];
        List<int> pathList = new List<int>();

        pathList.Add(s);

        printAllPathsUtil(s, d, isVisited, pathList);
    }
    private void printAllPathsUtil(int u, int d,
                                   bool[] isVisited,
                                   List<int> localPathList)
    {

        if (u.Equals(d))
        {
            
            StreamWriter writer = new StreamWriter(path, true);  
            writer.WriteLine(string.Join(" ", localPathList));
            writer.Close();
            return;
        }
        isVisited[u] = true;

        foreach (int i in adjList[u])
        {
            if (!isVisited[i])
            {
                localPathList.Add(i);
                printAllPathsUtil(i, d, isVisited,
                                  localPathList);
                localPathList.Remove(i);
            }
        }
        isVisited[u] = false;
    }
}