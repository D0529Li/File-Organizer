using System.Collections.Generic;
using System.IO;
using System;

namespace File_Organizer
{
    [Serializable]
    public class People
    {
        public List<Person> PeopleList = [];

        public People() { }

        public void AddPerson(string name, bool isPicture, int numOfFiles)
        {
            PeopleList.Add(new Person(name, isPicture, numOfFiles));
        }

        public void UpdatePeople(string path = "G:\\123")
        {
            // Folders
            var subDirectories = Directory.GetDirectories(path);
            foreach (var subDirectory in subDirectories)
            {
                var files = Directory.GetFiles(subDirectory);
                var numOfFiles = files.Length;
                var name = subDirectory.Substring(subDirectory.LastIndexOf('\\') + 1);
                AddPerson(name, true, numOfFiles);
            }

            // Files
            foreach (var file in Directory.GetFiles(path))
            {
                if (!file.EndsWith(".mp4"))
                    continue;
                var name = file[(file.LastIndexOf('\\') + 1)..(file.LastIndexOf(".mp4"))];
                AddPerson(name, false, 1);
            }
        }

        public Person DrawPerson()
        {
            var random = new Random();
            var index = random.Next(PeopleList.Count);
            return PeopleList[index];
        }
    }

    public class Person
    {
        public string? Name { get; set; }
        public bool IsFolder { get; set; }
        public int NumOfFiles { get; set; }

        public Person() { }

        public Person(string name, bool isFolder, int numOfFiles)
        {
            Name = name;
            IsFolder = isFolder;
            NumOfFiles = numOfFiles;
        }
    }
}
