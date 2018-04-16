using System.Text.RegularExpressions;
﻿using System;
using System.IO;
using System.Text;
using Gtk;
using System.Collections.Generic;

public partial class MainWindow: Gtk.Window{

	//===========================
	// Structures               |
	//===========================
	public struct Token{
		public string lexeme;
		public string type;
		public Token(string p1, string p2){
			lexeme = p1;
			type = p2;
		}
	}
	public struct Symbol{
		public string Variable;
		public string Value;
		public string Type;
		public Symbol(string p1, string p2, string p3){
			Variable = p1;
			Value = p2;
			Type = p3;
		}
	}
	//===========================
	// End of Structures        |
	//===========================



	//===========================
	// Regex                    |
	//===========================
	public Regex varidentreg = new Regex("^[A-Za-z][A-Za-z0-9_]*$");
	public Regex numbrreg = new Regex("^-?[0-9]+$");
	public Regex numbarreg = new Regex("^-?[0-9]+\\.[0-9]+$");
	public Regex yarnreg0 = new Regex("^\"([^\"]*)\"$");
	public Regex yarnreg1 = new Regex("^\"([^\"]*)$");
	public Regex yarnreg2 = new Regex("^([^\"]*)\"$");
	public Regex troofreg = new Regex("^(WIN|FAIL)$");
	public Regex keywordreg = new Regex("^((HAI)|(KTHXBYE)|(I)|(ITZ)|(R)|(SUM)|(DIFF)|(PRODUKT)|(QUOSHUNT)|(MOD)|(BIGGR)|(SMALLR)|(BOTH)|(EITHER)|(WON)|(NOT)|(ANY)|(ALL)|(BOTH)|(DIFFRINT)|(SMOOSH)|(MAEK)|(AN)|(IS)|(VISIBLE)|(GIMMEH)|(O)|(YA)|(MEBBE)|(NO)|(OIC)|(WTF\\?)|(OMG)|(OMGWTF)|(UPPIN)|(NERFIN)|(YR)|(TIL)|(WILE)|(IM)|(GTFO)|(MKAY))$");
	public Regex kwoperationreg = new Regex("^(NOT)|(SUM)|(DIFF)|(PRODUKT)|(QUOSHUNT)|(MOD)|(BIGGR)|(SMALLR)|(BOTH)|(EITHER)|(WON)|(ANY)|(ALL)|(DIFFRINT)$");//OF
	public Regex kwassignmentreg = new Regex("^I$");
	public Regex kwloopreg = new Regex("^IM$");
	public Regex kwtypecastreg = new Regex("^IS$");
	public Regex kwifelsereg = new Regex("^O$");
	public Regex kwifthenreg = new Regex("^YA$");
	public Regex kwelsereg = new Regex("^NO$");
	public Regex kwcommentreg = new Regex("BTW");
	public Regex kwmulticommentreg = new Regex("^OBTW$");
	public Regex kwmulticommentdelreg = new Regex("^TLDR$");
	public Regex kwsoftdelimiterreg = new Regex ("^,$");
	//===========================
	// End of Regex             |
	//===========================



	//===========================
	// Global Variables         |
	//===========================
	ListStore token_list_view = new ListStore(typeof(string),typeof(string));
	List<Token> token_list = new List<Token>();
	ListStore symbol_list_view = new ListStore(typeof(string),typeof(string),typeof(string));
	List<Symbol> symbol_list = new List<Symbol>();
	private int c;
	private bool ErrorDetected = false;
	private int semanticlinecounter = 1;
	//===========================
	// End of Global Variables  |
	//===========================



	//===========================
	// Main Window              |
	//===========================
	public MainWindow () : base (Gtk.WindowType.Toplevel){
		Build ();
		lexemelist.Model = token_list_view;
		lexemelist.AppendColumn("Lexeme",new CellRendererText(),"text",0);
		lexemelist.AppendColumn("Type",new CellRendererText(),"text",1);
		symboltable.Model = symbol_list_view;
		symboltable.AppendColumn("Variable",new CellRendererText(),"text",0);
		symboltable.AppendColumn("Value",new CellRendererText(),"text",1);
		symboltable.AppendColumn("Type",new CellRendererText(),"text",2);
	}
	//===========================
	// End of Main Window       |
	//===========================



	//===========================
	// GUI Functions            |
	//===========================
	protected void OnDeleteEvent (object sender, DeleteEventArgs a){
		Application.Quit ();
		a.RetVal = true;
	}
	protected void OnFilebuttonSelectionChanged (object sender, EventArgs e){
		try{   // Open the text file using a stream reader.
			using (StreamReader sr = new StreamReader(filebutton.Filename)){
				// Read the stream to a string, and write the string to the console.
				String fire = sr.ReadToEnd();
				codearea.Buffer.Text = fire;
			}
		}
		catch{
			commandline.Buffer.Text = "The file could not be read:";
		}
	}
	protected void OnExecutebuttonClicked (object sender, EventArgs e){
		ExecuteInitValues ();
		string s = codearea.Buffer.Text;
		//call lexical analyzer and construct symbol table
		if(!lexicalanalyzer(s)){
			return;
		};
		for (int i = 0; i < token_list.Count; i++) {
			token_list_view.AppendValues (token_list[i].lexeme,token_list[i].type);
		}
		//call semantic/syntax analyzer
		if(token_list[0].lexeme =="HAI"){
			try{codeReader(0,"KTHXBYE", "HAI");}
			catch{commandline.Buffer.Text = "Unknown Error";}
		}else{
			commandline.Buffer.Text += "Error: Expected 'HAI' on line 1";
			return;
		}

		foreach (Symbol symbol in symbol_list) {
			symbol_list_view.AppendValues (symbol.Variable,symbol.Value,symbol.Type);
		}
	}
	//===========================
	// End of GUI Functions     |
	//===========================



	//===========================
	// Initialization           |
	//===========================
	private void ExecuteInitValues(){
		semanticlinecounter = 1;
		ErrorDetected = false;
		token_list_view.Clear();
		token_list.Clear ();
		symbol_list.Clear ();
		symbol_list_view.Clear ();
		commandline.Buffer.Text = "";
		symbol_list.Add (new Symbol ("IT", "noot noot", "NOOB"));
	}
	//===========================
	// End of Initialization    |
	//===========================



	//===========================
	// Analyzers                |
	//===========================
	private bool lexicalanalyzer(string s){
		var slist = s.Split (new[] { "\n" }, StringSplitOptions.None);
		if (slist.Length == 0) {
			return false;
		}
		int multicommentflag = 0;
		//
		for (int z = 0; z < slist.Length; z++) {
			bool foundquote = false;
			string tempstring = "";
			var temp = new List<String>();

			//string catcher
			for(int i=0; i<slist[z].Length;i++){
				if(slist[z][i]=='\"'){
					foundquote = !foundquote;
				}
				if ((slist [z] [i] == ' '||slist [z] [i] == '\t') && foundquote == false) {
					if(tempstring.Length != 0){
						temp.Add (tempstring);
						tempstring = "";
					}
				} else {
					tempstring += slist [z] [i];
					if (i == slist [z].Length - 1 && !foundquote) {
						temp.Add (tempstring);
					}else if(i == slist [z].Length - 1 && foundquote){
						commandline.Buffer.Text += "Error: Expected \" at line "+ (z+1);
					}
				}
			}

			for (int y = 0; y < temp.Count; y++) {
				if (kwmulticommentdelreg.IsMatch (temp [y])) {
					token_list.Add (new Token (temp [y], "MULTICOMMENT DELIMITER"));
					multicommentflag = 0;
					continue;
				}
				if (multicommentflag == 1) {
					break;
				}
				if (kwmulticommentreg.IsMatch (temp [y])) {
					token_list.Add (new Token (temp [y], "MULTICOMMENT"));
					multicommentflag = 1;
					continue;
				}
				if(multicommentflag == 0){
				if (kwcommentreg.IsMatch (temp [y])) {
					token_list.Add (new Token (temp [y], "COMMENT"));
					break;
				}
				if (keywordreg.IsMatch (temp [y])) {
					if (kwoperationreg.IsMatch (temp [y])) {
						if (y + 1 < temp.Count && temp [y + 1] == "OF") {
							token_list.Add (new Token (temp [y] + " " + temp [y + 1], "OPERATOR"));
							y = y + 1;
						} else if (y + 1 < temp.Count && temp [y] == "BOTH" && temp [y + 1] == "SAEM") {
							token_list.Add (new Token (temp [y] + " " + temp [y + 1], "OPERATOR"));
							y = y + 1;
						} else if (temp [y] == "NOT" || temp [y] == "DIFFRINT") {
							token_list.Add (new Token (temp [y], "OPERATOR"));
						} else {
							token_list.Add (new Token (temp [y], "VAR IDENTIFIER"));
						}

					} else if (kwassignmentreg.IsMatch (temp [y])) {
						if (y + 2 < temp.Count && temp [y + 1] == "HAS" && temp [y + 2] == "A") {
							token_list.Add (new Token (temp [y] + " " + temp [y + 1] + " " + temp [y + 2], "VAR DECLARATION"));
							y = y + 2;
						} else {
							token_list.Add (new Token (temp [y], "VAR IDENTIFER"));

						}

					} else if (kwloopreg.IsMatch (temp [y])) {
						if (y + 2 < temp.Count && (temp [y + 1] == "IN" || temp [y + 1] == "OUTTA") && temp [y + 2] == "YR") {
							token_list.Add (new Token (temp [y] + " " + temp [y + 1] + " " + temp [y + 2], "LOOP"));
							y = y + 2;
						} else {
							token_list.Add (new Token (temp [y], "VAR IDENTIFIER"));
						}

					} else if (kwtypecastreg.IsMatch (temp [y])) {
						if (y + 2 < temp.Count && temp [y + 1] == "NOW" && temp [y + 2] == "A") {
							token_list.Add (new Token (temp [y] + " " + temp [y + 1] + " " + temp [y + 2], "TYPECAST"));
							y = y + 2;
						} else {
							token_list.Add (new Token (temp [y], "VAR IDENTIFIER"));
						}

					} else if (kwifelsereg.IsMatch (temp [y])) {
						if (y + 1 < temp.Count && temp [y + 1] == "RLY?") {
							token_list.Add (new Token (temp [y] + " " + temp [y + 1], "IF-ELSE"));
							y = y + 1;
						} else {
							token_list.Add (new Token (temp [y], "VAR IDENTIFIER"));
						}

					} else if (kwifthenreg.IsMatch (temp [y])) {
						if (y + 1 < temp.Count && temp [y + 1] == "RLY") {
							token_list.Add (new Token (temp [y] + " " + temp [y + 1], "IF-THEN"));
							y = y + 1;
						} else {
							token_list.Add (new Token (temp [y], "VAR IDENTIFIER"));
						}

					} else if (kwelsereg.IsMatch (temp [y])) {
						if (y + 1 < temp.Count && temp [y + 1] == "WAI") {
							token_list.Add (new Token (temp [y] + " " + temp [y + 1], "ELSE"));
							y = y + 1;
						} else {
							token_list.Add (new Token (temp [y], "VAR IDENTIFIER"));
						}

					} else {
						token_list.Add (new Token (temp [y], "KEYWORD"));
					}
				} else if (numbrreg.IsMatch (temp [y])) {
					token_list.Add (new Token (temp [y], "NUMBR LITERAL"));
				} else if (numbarreg.IsMatch (temp [y])) {
					token_list.Add (new Token (temp [y], "NUMBAR LITERAL"));
				} else if (yarnreg0.IsMatch (temp [y])) {
					token_list.Add (new Token (temp [y], "YARN LITERAL"));
				} else if (troofreg.IsMatch (temp [y])) {
					token_list.Add (new Token (temp [y], "TROOF LITERAL"));
				} else if (varidentreg.IsMatch (temp [y])) {
					token_list.Add (new Token (temp [y], "VAR IDENTIFIER"));
				} else {
					commandline.Buffer.Text = commandline.Buffer.Text + "Error on line " + (z + 1) + ": '" + temp [y] + "' unknown identifier\n";
				}
			}		
			};
			token_list.Add(new Token("\\n", "DELIMITER"));
		}
		return true;
	}
	private void codeReader(int i, string exitcode, string startcode){
		c = i;
		bool foundexitcode = false;
		bool breakoutacases = false;
		string ifend = "";
		int j;
		i+=2;
		if(startcode == "O RLY?"){
			for (j=i;j<token_list.Count; j++){
				if (token_list[j].lexeme == "YA RLY"){
					ifend = "NO WAI";
					break;
				}
			}
			j+=2;
			if (ifend == ""){
				ErrorDetected = true;
				commandline.Buffer.Text += "SyntaxError: \"O RLY?\" Block missing \"YA RLY\" clause ";
				return;
			}

			ifend = "";
			for (;i<token_list.Count; i++){
				if (token_list[i].lexeme == "NO WAI"){
					ifend = "OIC";
					break;
				}
			}
			i+=2;
			if (ifend == ""){
				ErrorDetected = true;
				commandline.Buffer.Text += "SyntaxError: \"O RLY?\" Block missing \"NO WAI\" clause ";
				return;
			}
			var q=getVariableValue("IT");
			if (q!="FAIL" && q!="0" && q!="" && q!="noot noot"){
				ifend = "NO WAI";
				i=j;
			}else{
				ifend = "OIC";
			}
			c = i;
		}else if(startcode=="WTF?"){
			ifend = "OIC";
			for(;i<token_list.Count;i++){
				if(token_list[i].lexeme == "OMG" || token_list[i].lexeme == "OMGWTF"){
					if(token_list[i].lexeme != "OMGWTF"){
						if(token_list[i+1].lexeme == getVariableValue("IT")){
							break;
						}
						i++;
					}else{
						break;
					}
				}
			}
			i+=2;
			c=i;

		}
		
		for (; i < token_list.Count; i++) {
			if(breakoutacases){
				for (;i < token_list.Count;++i){
					if (token_list[i].type == "DELIMITER"){
						semanticlinecounter++;
					}else if (token_list[i].lexeme == "OIC"){
						foundexitcode = true;
						break;
					}
				}
				c = i+2;
				break;
			}
			if(token_list[i].lexeme == "O RLY?"){
				codeReader(i, "OIC", "O RLY?");
			}else if(token_list[i].lexeme == "WTF?"){
				codeReader(i,"OIC", "WTF?");
			}else if(token_list[i].lexeme == "I HAS A"){
				declaration (i);
			}else if (token_list [i].type == "OPERATOR") {
				operation (i);
			} else if (token_list [i].lexeme == "VISIBLE") {
				visible (i);
			}else if(token_list [i].lexeme == "GIMMEH"){
				gimmeh (i);
			}else if(token_list[i].type == "VAR IDENTIFIER"){
				R(i);
			}else if(token_list[i].type == "NUMBAR LITERAL" || token_list[i].type == "NUMBR LITERAL" || token_list[i].type == "YARN LITERAL" ||token_list[i].type == "TROOF LITERAL"){
				editVariable("IT", token_list[i].type, token_list[i].lexeme);
				c+=2;
			}else if(token_list[i].lexeme == exitcode){
				foundexitcode = true;
				c+=2;
				break;
			}else if(token_list[i].type == "DELIMITER"){
				semanticlinecounter++;
				c+=1;
				continue;
			}else if((token_list[i].lexeme == "GTFO" && startcode =="WTF?")||token_list[i].lexeme == ifend){
				breakoutacases = true;
				continue;
			}else if((token_list[i].lexeme == "OMG" || token_list[i].lexeme == "OMGWTF") && startcode =="WTF?"){
				if (token_list[i].lexeme == "OMG")
					c+=3;
				else
					c+=2;
			}else{
				ErrorDetected = true;
				commandline.Buffer.Text += "SyntaxError: KeyWord: \"" + token_list [i].lexeme + "\" not allowed " + startcode;
				if (startcode != "HAI")
					return;
			}
			i = c-1;
			semanticlinecounter++;

			if (ErrorDetected && startcode == "HAI") {
				commandline.Buffer.Text += "on Line " + semanticlinecounter + "\n";
				return;
			}else if(ErrorDetected){
				return;
			}
			if (token_list [c - 1].type != "DELIMITER") {
				commandline.Buffer.Text += "SyntaxError: Unexpected " + token_list[c-1].type +": "+ token_list[c-1].lexeme +"\n";
			}
		}

		if (!foundexitcode){
			ErrorDetected = true;
			commandline.Buffer.Text += "SyntaxError: Expected End-Clause: \"" + exitcode + "\" for \"" + startcode + "\" ";
			c = i+2;
		}else{
			c = i+2;
		}
	}
	//===========================
	// End of Analyzers         |
	//===========================
	


	//===========================
	// Program Functions        |
	//===========================
	private void gimmeh(int i){
		c = i;
		string s, t;
		if(token_list[i+1].type != "VAR IDENTIFIER"){
			ErrorDetected = true;
			commandline.Buffer.Text += "SyntaxError: Expected variable name ";
			return;
		}
		s = Prompt.ShowDialog();
		if (numbrreg.IsMatch (s)) {
			t = "NUMBR LITERAL";
		} else if (numbarreg.IsMatch (s)) {
			t = "NUMBAR LITERAL";
		} else if (yarnreg0.IsMatch ("\""+s+"\"")) {
			s = "\"" + s + "\"";
			t = "YARN LITERAL";
		} else {
			commandline.Buffer.Text += "SemanticError: GIMMEH error received \" within input ";
			ErrorDetected = true;
			return;
		}
		editVariable (token_list[i+1].lexeme, t, s);
		if (ErrorDetected) {
			return;
		}
		c += 3;
	}
	private void declaration(int i){
		c = i;
		if (token_list [i + 1].type == "VAR IDENTIFIER") {
			checkDuplicateVariable (token_list[i+1].lexeme);
			if (token_list [i + 2].lexeme == "ITZ") {
				c += 2;
				if(token_list[i+3].type=="NUMBR LITERAL"){
					symbol_list.Add (new Symbol (token_list [i + 1].lexeme, token_list[i+3].lexeme, token_list[i+3].type));
				}else if(token_list[i+3].type=="NUMBAR LITERAL"){
					symbol_list.Add (new Symbol (token_list [i + 1].lexeme, token_list[i+3].lexeme, token_list[i+3].type));
				}else if(token_list[i+3].type=="YARN LITERAL"){
					symbol_list.Add (new Symbol (token_list [i + 1].lexeme, token_list[i+3].lexeme, token_list[i+3].type));
				}else if(token_list[i+3].type=="TROOF LITERAL"){
					symbol_list.Add (new Symbol (token_list [i + 1].lexeme, token_list[i+3].lexeme, token_list[i+3].type));
				}else if(token_list[i+3].type=="OPERATOR"){
					operation (i + 3);
					var temp = getVariableValue("IT");
					string s;
					if(numbrreg.IsMatch(temp)){s = "NUMBR LITERAL";}
					else if(numbarreg.IsMatch(temp)){s = "NUMBAR LITERAL";}
					else if(yarnreg0.IsMatch(temp)){s = "YARN LITERAL";}
					else if(troofreg.IsMatch(temp)){s = "TROOF LITERAL";}
					else {s = "UNKNOWN";}
					symbol_list.Add (new Symbol (token_list [i + 1].lexeme,temp, s));
					c-=3;
				}else if(token_list[i+3].type=="VAR IDENTIFIER"){
					var temp = getVariable(token_list[i+3].lexeme);
					if (ErrorDetected){return;}
					symbol_list.Add (new Symbol (token_list[i+1].lexeme, temp.Value, temp.Type));
				}
			} else {
				symbol_list.Add (new Symbol (token_list [i + 1].lexeme, "", "NOOB"));
			}
			c += 3;
		} else {
			ErrorDetected = true;
			commandline.Buffer.Text += "Missing Var Identifier at line "+semanticlinecounter+"\n";
			return;
		}
	}
	private void operation(int i){
		c = i;
		dynamic x, y;
		string loltype = "NUMBR LITERAL";

		if (token_list [i + 1].type == "OPERATOR") {
			operation (i + 1);
			if (ErrorDetected) {
				return;
			}
			x = getVariableValue ("IT");
		} else if (token_list [i + 1].type == "YARN LITERAL" || token_list [i + 1].type == "TROOF LITERAL" || token_list [i + 1].type == "NUMBAR LITERAL" || token_list [i + 1].type == "NUMBR LITERAL") {
			//error
			x = token_list [i + 1].lexeme;
			c += 3;
		} else if (token_list [i + 1].type == "VAR IDENTIFIER" || token_list [i + 1].lexeme == "IT") {//if variable name
			x = getVariable(token_list [i + 1].lexeme);
			if (ErrorDetected) {return;}
			if(x.Type == "NOOB"){
				ErrorDetected = true;
				commandline.Buffer.Text += "TypeError: Cannot implicitly typecast NOOB ";
				return;
			}
			x = x.Value;
			c += 3;
		} else if(token_list[i+1].type == "KEYWORD"){
			x = 0;
			ErrorDetected = true;
			commandline.Buffer.Text += "SyntaxError: Unexpected Keyword \"" + token_list[i+1].lexeme+ "\" ";
			return;
		} else {
			x = 0;
			ErrorDetected = true;
			commandline.Buffer.Text += "SyntaxError: Expected parameters, got NONE ";
			return;
		}

		if (token_list [i].lexeme == "NOT") {
			//if tokens[c-1]!="\n" error
			if (x=="FAIL" || x == "0"){
				editVariable("IT", "TROOF LITERAL", "WIN");
			}else{
				editVariable("IT", "TROOF LITERAL", "FAIL");
			}
			return;
		}else if(token_list[i].lexeme == "ALL OF"){
			String tempbool = "WIN";
			Symbol tempsymbol;
			i++;
			for(;token_list[i].type !="DELIMITER";i++){
				if(token_list [i].lexeme == "WIN"){
					//do nothing
				}else if (token_list [i].lexeme == "FAIL") {
					tempbool = "FAIL";
				}else if (token_list [i].type == "VAR IDENTIFIER") {
					tempsymbol = getVariable (token_list [i].lexeme);
					if (ErrorDetected){return;}
					if (tempsymbol.Value == "FAIL" || tempsymbol.Value == "0") {
						tempbool = "FAIL";
					}
				}else if (token_list [i].type == "OPERATOR") {
					operation (i);
					tempsymbol = getVariable ("IT");
					if (tempsymbol.Value == "FAIL" || tempsymbol.Value == "0") {
						tempbool = "FAIL";
					}
					i = c - 2;
				}else if (token_list [i].lexeme == "MKAY") {
					c = i + 3;
					break;
				}else{
					ErrorDetected = true;
					commandline.Buffer.Text += "SyntaxError: Expected Expression, got " + token_list [i].type + " ";
					return;
				}
				c++;
			}
			c-=1;
			editVariable("IT","TROOF LITERAL",tempbool);
			return;
		}else if(token_list[i].lexeme == "ANY OF"){
			String tempbool = "FAIL";
			Symbol tempsymbol;
			i++;
			for(;token_list[i].type !="DELIMITER";i++){
				if (token_list[i].lexeme == "FAIL"){
					// do nothing
				}else if (token_list [i].lexeme == "WIN") {
					tempbool = "WIN";
				}else if (token_list [i].type == "VAR IDENTIFIER") {
					tempsymbol = getVariable (token_list [i].lexeme);
					if (ErrorDetected){return;}
					if (tempsymbol.Value != "FAIL" && tempsymbol.Value != "0") {
						tempbool = "WIN";
					}
				}else if (token_list [i].type == "OPERATOR") {
					operation (i);
					tempsymbol = getVariable ("IT");
					if (tempsymbol.Value != "FAIL" && tempsymbol.Value != "0") {
						tempbool = "WIN";
					}
					i = c - 2;
				}else if (token_list [i].lexeme == "MKAY") {
					c = i + 3;
					break;
				}else{
					ErrorDetected = true;
					commandline.Buffer.Text += "SyntaxError: Expected Expression, got "  + token_list [i].type + " ";
					return;
				}
				c++;
			}
			c-=1;
			editVariable("IT","TROOF LITERAL",tempbool);
			return;
		}

		if (token_list [c - 1].lexeme != "AN") {
			commandline.Buffer.Text += "SyntaxError: Expected AN, got \'" + token_list [c - 1].lexeme + "\' ";
			ErrorDetected = true;
			return;
		}

		if (token_list [c].type == "OPERATOR") {
			operation (c);
			y = getVariableValue("IT");
		}else if (token_list [c].type == "YARN LITERAL" || token_list [c].type == "TROOF LITERAL" || token_list [c].type ==  "NUMBAR LITERAL" || token_list [c].type ==  "NUMBR LITERAL") {
			y = token_list [c].lexeme;
			c += 2;
		} else if (token_list [c].type == "VAR IDENTIFIER" || token_list [c].lexeme == "IT"){//if variable name
			y = getVariable(token_list [c].lexeme);
			if (ErrorDetected) {return;}
			if(y.Type == "NOOB"){
				ErrorDetected = true;
				commandline.Buffer.Text += "TypeError: Cannot implicitly typecast NOOB ";
				return;
			}
			y = y.Value;
			c += 2;
		} else if(token_list[c].type == "KEYWORD"){
			y = 0;
			ErrorDetected = true;
			commandline.Buffer.Text += "SyntaxError: Unexpected Keyword \"" + token_list[c].lexeme+ "\" ";
			return;
		} else {
			y = 0;
			ErrorDetected = true;
			commandline.Buffer.Text += "SyntaxError: Expected 2 parameters, got 1 ";
			return;
		};

		if (token_list [i].lexeme == "BOTH SAEM") {
			if (x == y) {
				editVariable("IT", "TROOF LITERAL", "WIN");
			}else{
				editVariable("IT", "TROOF LITERAL", "FAIL");
			}
			return;
		}else if(token_list [i].lexeme == "DIFFRINT"){
			if (x != y) {
				editVariable("IT", "TROOF LITERAL", "WIN");
			}else{
				editVariable("IT", "TROOF LITERAL", "FAIL");
			}
			return;
		} else if (token_list [i].lexeme == "BOTH OF") {
			if (x=="0"||y=="0"||x=="FAIL"||y=="FAIL"){
				editVariable("IT", "TROOF LITERAL", "FAIL");
			}else{
				editVariable("IT", "TROOF LITERAL", "WIN");
			}
			return;
		} else if (token_list [i].lexeme == "EITHER OF") {
			if ((x=="FAIL"|| x=="0") && (y=="FAIL"|| y=="0")){
				editVariable("IT", "TROOF LITERAL", "FAIL");
			}else{
				editVariable("IT", "TROOF LITERAL", "WIN");
			}
			return;
		}

		//convert x
		if (x=="WIN"){
			x = 1;
		}else if(x=="FAIL"){
			x = 0;
		}else if(yarnreg0.IsMatch(x)){
			x = x.Substring(1,x.Length-2);
			if(numbrreg.IsMatch(x)){
				x = Convert.ToInt32 (x);
			}else if(numbarreg.IsMatch(x)){
				x = Convert.ToDouble (x);
				loltype = "NUMBAR LITERAL";
			}else{
				ErrorDetected = true;
				commandline.Buffer.Text += "TypeError: Cannot implicitly typecast \"" + x + "\" ";
				return;
			}
		}else if(numbrreg.IsMatch(x)){
			x = Convert.ToInt32 (x);
		}else if(numbarreg.IsMatch(x)){
			x = Convert.ToDouble (x);
			loltype = "NUMBAR LITERAL";
		}

		//convert y
		if (y=="WIN"){
			y = 1;
		}else if(y=="FAIL"){
			y = 0;
		}else if(yarnreg0.IsMatch(y)){
			y = y.Substring(1,y.Length-2);
			if(numbrreg.IsMatch(y)){
				y = Convert.ToInt32 (y);
			}else if(numbarreg.IsMatch(y)){
				y = Convert.ToDouble (y);
				loltype = "NUMBAR LITERAL";
			}else{
				ErrorDetected = true;
				commandline.Buffer.Text += "TypeError: Cannot implicitly typecast \"" + y + "\" ";
				return;
			}
		}else if(numbrreg.IsMatch(y)){
			y = Convert.ToInt32 (y);
		}else if(numbarreg.IsMatch(y)){
			y = Convert.ToDouble (y);
			loltype = "NUMBAR LITERAL";
		}


		if (token_list [i].lexeme == "SUM OF") {
			x+=y;
		} else if (token_list [i].lexeme == "DIFF OF") {
			x-=y;
		} else if (token_list [i].lexeme == "PRODUKT OF") {
			x*=y;
		} else if (token_list [i].lexeme == "QUOSHUNT OF") {
			if (y == 0) {
				ErrorDetected = true;
				commandline.Buffer.Text += "SemanticError: Cannot divide by 0 ";
				return;
			}
			x/=y;
		} else if (token_list [i].lexeme == "MOD OF") {
			if (y == 0) {
				ErrorDetected = true;
				commandline.Buffer.Text += "SemanticError: Cannot divide by 0 ";
				return;
			}
			x%=y;
		} else if (token_list [i].lexeme == "BIGGR OF") {
			if (x < y) {
				x = y;
			}
		} else if (token_list [i].lexeme == "SMALLR OF") {
			if (x > y) {
				x = y;
			}
		}
		if (loltype == "NUMBAR LITERAL"){
			x*=1.0;
		}
		editVariable("IT", loltype, x+"");
	}
	private void visible(int i){
		int temp = i;
		c = i;
		dynamic t;
		string s = "";
		i++;
		for (; i < token_list.Count && token_list [i].type != "DELIMITER"; ++i) {
			if (token_list [i].type == "OPERATOR") {
				operation (i);
				i = c-2;
				if (ErrorDetected) {return;}
				s += getVariableValue("IT");
			} else if (token_list [i].lexeme == "SMOOSH") {
				smoosh(i);
				i = c-2;
				if (ErrorDetected) {return;}
				s += getVariableValue("IT");
			} else if (token_list [i].type == "NUMBR LITERAL" || token_list [i].type == "NUMBAR LITERAL" || token_list [i].type == "YARN LITERAL" || token_list [i].type == "TROOF LITERAL") {
				if (token_list [i].type == "YARN LITERAL"){
					t = token_list [i].lexeme;
					s += t.Substring(1,t.Length-2);
				}
				else{
					s += token_list [i].lexeme;
				}
			} else if (token_list [i].type == "VAR IDENTIFIER") {//if variable name
				t = getVariable(token_list [i].lexeme);
				if (ErrorDetected) {return;}
				if (t.Type == "NOOB") {
					s += "NOOB";
				}else if(t.Type == "YARN LITERAL"){
					t = t.Value;
					s += t.Substring(1,t.Length-2);
				} else {
					s += t.Value;
				}
			} else {
				ErrorDetected = true;
				commandline.Buffer.Text += "SyntaxError: Expected Expression, got \""+token_list [i].lexeme + "\" ";
				return;
			}
		}
		c = i+1;
		commandline.Buffer.Text += s + "\n";
	}
	private void R(int i){
		c=i;
		dynamic t;
		if(token_list[i+1].type == "DELIMITER"){
			t = getVariable(token_list[i].lexeme);
			if (ErrorDetected){return;}
			editVariable("IT", t.Type, t.Value);
			c +=2;
		}
		else if(token_list[i+1].lexeme == "R"){
			if(token_list[i+3].type == "DELIMITER"){
				if(token_list[i+2].type == "VAR IDENTIFIER"){
					
					dynamic x = getVariable(token_list[i+2].lexeme);
					if (ErrorDetected){return;}

					dynamic y = getVariable(token_list[i].lexeme);
					if (ErrorDetected){return;}

					editVariable(y.Variable,x.Type, x.Value);
					if (ErrorDetected){return;}
					
					c+=4;
				}	
				else if(token_list[i+2].type != "KEYWORD"){

					dynamic y = getVariable(token_list[i].lexeme);
					if (ErrorDetected){return;}
					
					editVariable(y.Variable,token_list[i+2].type,token_list[i+2].lexeme);
					if (ErrorDetected){return;}
					
						c+=4;
				}
			}
			else if(token_list[i+2].type == "OPERATOR"){
				string s;
				operation(i+2);

				dynamic y = getVariable(token_list[i].lexeme);
				if (ErrorDetected){return;}
				
				var temp = getVariable("IT");
				if (ErrorDetected){return;}

				editVariable(y.Variable,temp.Type,temp.Value);
				if (ErrorDetected){return;}
			}
		} else {
			commandline.Buffer.Text += "SyntaxError: Unexpected "+ token_list[i+1].type+" ";
			ErrorDetected=true;
		}
	}
	private void smoosh(int i){
		int temp = i;
		c = i;
		dynamic t;
		string s = "";
		i++;
		for (; i < token_list.Count && token_list [i].type != "DELIMITER" && token_list [i].lexeme !="MKAY"; ++i) {
			if (token_list [i].type == "OPERATOR") {
				operation (i);
				i = c-2;
				if (ErrorDetected) {return;}
				s += getVariableValue("IT");
			} else if (token_list [i].type == "NUMBR LITERAL" || token_list [i].type == "NUMBAR LITERAL" || token_list [i].type == "YARN LITERAL" || token_list [i].type == "TROOF LITERAL") {
				if (token_list [i].type == "YARN LITERAL"){
					t = token_list [i].lexeme;
					s += t.Substring(1,t.Length-2);
				}
				else{
					s += token_list [i].lexeme;
				}
			} else if (token_list [i].type == "VAR IDENTIFIER") {//if variable name
				t = getVariable(token_list [i].lexeme);
				if (ErrorDetected) {return;}
				if (t.Type == "NOOB") {
					s += "NOOB";
				}else if(t.Type == "YARN LITERAL"){
					t = t.Value;
					s += t.Substring(1,t.Length-2);
				} else {
					s += t.Value;
				}
			} else {
				ErrorDetected = true;
				commandline.Buffer.Text += "SyntaxError: Expected Expression, got \""+token_list [i].lexeme + "\" ";
				return;
			}
		}
		c = i+1;
		editVariable("IT", "YARN LITERAL", s);
	}
	//===========================
	// End of Program Functions |
	//===========================



	//=============================
	// Variable Operations        |
	//=============================
	private void checkDuplicateVariable(String str){
		for (int i = 0; i < symbol_list.Count; i++) {
			if (symbol_list[i].Variable == str) {
				symbol_list.RemoveAt (i);

			}
		}
	}
	private Symbol getVariable(string str){
		for (int i = 0; i < symbol_list.Count; i++) {
			if (symbol_list[i].Variable == str) {
				return symbol_list [i];
			}
		}
		ErrorDetected = true;
		commandline.Buffer.Text += "NameError: Accessing undeclared variable " + str + " ";
		return new Symbol ("", "", "");
	}
	private void editVariable(string str, string type, string value){
		for (int i = 0; i < symbol_list.Count; i++) {
			if (symbol_list[i].Variable == str) {
				symbol_list.RemoveAt (i);
				symbol_list.Add (new Symbol (str, value, type));
				return;
			}
		}
		ErrorDetected = true;
		commandline.Buffer.Text += "NameError: Accessing undeclared variable " + str + " ";
	}
	private string getVariableValue(string str){
		for (int i = 0; i < symbol_list.Count; i++) {
			if (symbol_list[i].Variable == str) {
				return symbol_list [i].Value;
			}
		}
		ErrorDetected = true;
		commandline.Buffer.Text += "NameError: Accessing undeclared variable " + str + " ";
		return "";
	}
	//=============================
	// End of Variable Operations |
	//=============================

}