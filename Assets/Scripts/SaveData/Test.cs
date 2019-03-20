using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

    public class Person {
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private int level;
        public int Level
        {
            get { return level; }
            set { level = value; }
        }
    }

	void Start () {
        //定义存档路径.
        string dirpath = Application.persistentDataPath + "/Save";
        //创建存档文件夹.
        IOHelper.CreateDirectory(dirpath);
        //定义存档文件路径.
        string fileName = dirpath + "/GameData.sav";
        Person t = new Person();
        t.Name = "daqipao";
        t.Level = 15;
        //保存数据.
        IOHelper.SetData(fileName, t, "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
        //读取数据.
        Person p1 = (Person)IOHelper.GetData(fileName, typeof(Person), "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
        Debug.Log(p1.Name);
        Debug.Log(p1.Level);

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
