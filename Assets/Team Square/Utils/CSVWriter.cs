using UnityEngine;
using System.IO;


public class CSVWriter : MonoBehaviour
{
    #region Singleton
    public static CSVWriter instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    #endregion


    private string cvsNameField = "TDL";
    private string fileName;
    private int csvCount = 0;


    public void WriteCSV(string header)
    {
        /// Create File
        fileName = Application.dataPath + "/CSV/" + cvsNameField + "_0" + ".csv";

        csvCount = 0;

        while (System.IO.File.Exists(fileName))
        {
            Debug.Log($"TW {fileName} already exist");

            csvCount++;
            fileName = Application.dataPath + "/CSV/" + cvsNameField + "_" + csvCount + ".csv";

            if (csvCount > 20)
            {
                break;
            }
        }



        TextWriter tw = new StreamWriter(fileName);

        tw.WriteLine(header);

        // foreach (var item in collection)
        // {
        //     string line = "";
        //     tw.WriteLine(line);
        // }
        tw.Close();

        Debug.LogWarning($"CSV {cvsNameField} wrote !");
    }

    public void UpdateCVSFileName(string newNAme)
    {
        cvsNameField = newNAme;
    }
}
