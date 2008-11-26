' Download psi-ms.obo
psimsURL = "http://psidev.cvs.sourceforge.net/*checkout*/psidev/psi/psi-ms/mzML/controlledVocabulary/psi-ms.obo"
psimsDestination = "..\..\pwiz\data\msdata\psi-ms.obo"
Set objXMLHTTP = CreateObject("MSXML2.XMLHTTP")

objXMLHTTP.open "GET", psimsURL, false
objXMLHTTP.send()

If objXMLHTTP.Status = 200 Then
  Set objADOStream = CreateObject("ADODB.Stream")
  objADOStream.Open
  objADOStream.Type = 1 'adTypeBinary

  objADOStream.Write objXMLHTTP.ResponseBody
  objADOStream.Position = 0    'Set the stream position to the start

  Set objFSO = Createobject("Scripting.FileSystemObject")
    If objFSO.Fileexists(psimsDestination) Then objFSO.DeleteFile psimsDestination
  Set objFSO = Nothing

  objADOStream.SaveToFile psimsDestination
  objADOStream.Close
  Set objADOStream = Nothing
End if

' Download unit.obo
unitURL = "http://obo.cvs.sourceforge.net/*checkout*/obo/obo/ontology/phenotype/unit.obo"
unitDestination = "..\..\pwiz\data\msdata\unit.obo"
objXMLHTTP.open "GET", unitURL, false
objXMLHTTP.send()

If objXMLHTTP.Status = 200 Then
  Set objADOStream = CreateObject("ADODB.Stream")
  objADOStream.Open
  objADOStream.Type = 1 'adTypeBinary

  objADOStream.Write objXMLHTTP.ResponseBody
  objADOStream.Position = 0    'Set the stream position to the start

  Set objFSO = Createobject("Scripting.FileSystemObject")
    If objFSO.Fileexists(unitDestination) Then objFSO.DeleteFile unitDestination
  Set objFSO = Nothing

  objADOStream.SaveToFile unitDestination
  objADOStream.Close
  Set objADOStream = Nothing
End if

Set objXMLHTTP = Nothing