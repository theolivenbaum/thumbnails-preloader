using System;
using System.Collections.Generic;
using System.IO;

namespace ThumbsPreloader.Core;

public sealed class DirectoryScanner
{
    private readonly string _path;
    private readonly bool _includeNestedDirectories;

    public DirectoryScanner(string path, bool includeNestedDirectories)
    {
        _path = path;
        _includeNestedDirectories = includeNestedDirectories;
    }

    public IEnumerable<string> GetItems()
    {
        return _includeNestedDirectories ? GetItemsNested() : GetItemsOnlyFirstLevel();
    }

    public IEnumerable<int> GetItemsCount()
    {
        return _includeNestedDirectories ? GetItemsCountNested() : GetItemsCountOnlyFirstLevel();
    }

    private IEnumerable<string> GetItemsOnlyFirstLevel()
    {
        string[]? items = null;
        try
        {
            items = Directory.GetFileSystemEntries(_path);
        }
        catch (Exception)
        {
            // Ignored.
        }

        if (items == null) yield break;
        foreach (var item in items) yield return item;
    }

    private IEnumerable<string> GetItemsNested()
    {
        var queue = new Queue<string>();
        queue.Enqueue(_path);
        while (queue.Count > 0)
        {
            var currentPath = queue.Dequeue();
            yield return currentPath;
            string[]? files = null;
            try
            {
                foreach (var subDirectory in Directory.GetDirectories(currentPath))
                    queue.Enqueue(subDirectory);
                files = Directory.GetFiles(currentPath);
            }
            catch (Exception)
            {
                // Ignored.
            }

            if (files == null) continue;
            foreach (var file in files) yield return file;
        }
    }

    private IEnumerable<int> GetItemsCountOnlyFirstLevel()
    {
        var itemsCount = 0;
        try
        {
            itemsCount = Directory.GetFileSystemEntries(_path).Length;
        }
        catch (Exception)
        {
            // Ignored.
        }

        if (itemsCount > 0) yield return itemsCount;
    }

    private IEnumerable<int> GetItemsCountNested()
    {
        var queue = new Queue<string>();
        queue.Enqueue(_path);
        while (queue.Count > 0)
        {
            var currentPath = queue.Dequeue();
            var itemsCount = 0;
            try
            {
                foreach (var subDir in Directory.GetDirectories(currentPath))
                {
                    queue.Enqueue(subDir);
                    itemsCount++;
                }
                itemsCount += Directory.GetFiles(currentPath).Length;
            }
            catch (Exception)
            {
                // Ignored.
            }

            if (itemsCount > 0) yield return itemsCount;
        }
    }
}
