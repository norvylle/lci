using System;
using System.Windows.Forms;

public static class Prompt
{
	public static string ShowDialog()
	{
		//MOST OF THE CODE FROM STACKOVERFLOW
		Form prompt = new Form();
		prompt.Width = 200;
		prompt.Height = 120;
		TextBox inputBox = new TextBox () { Left = 50, Top=10, Width=100, Height=30 };
		Button confirmation = new Button() { Text = "Ok", Left=50, Width=100, Top=50 };
		confirmation.Click += (sender, e) => { prompt.Close(); };
		prompt.Controls.Add(confirmation);
		prompt.Controls.Add(inputBox);
		prompt.ShowDialog();
		return inputBox.Text;
	}
}