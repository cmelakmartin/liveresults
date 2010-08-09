﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Data.OleDb;
using System.Data;
using System.Globalization;

namespace WOCEmmaClient
{
    public class OlaParser : IExternalSystemResultParser
    {
        private IDbConnection m_Connection;
        private int m_EventID;
        private int m_EventRaceId;

        public event ResultDelegate OnResult;
        public event LogMessageDelegate OnLogMessage;

        private bool m_Continue = false;
        public OlaParser(IDbConnection conn, int eventID, int eventRaceId)
        {
            m_Connection = conn;
            m_EventID = eventID;
            m_EventRaceId = eventRaceId;
        }

        private void FireOnResult(int id, int SI, string name, string club, string Class, int start, int time, int status, List<ResultStruct> results)
        {
            if (OnResult != null)
            {
                OnResult(id, SI, name, club, Class, start, time, status, results);
            }
        }
        private void FireLogMsg(string msg)
        {
            if (OnLogMessage != null)
                OnLogMessage(msg);
        }

        System.Threading.Thread th;

        public void Start()
        {
            m_Continue = true;
            th = new System.Threading.Thread(new System.Threading.ThreadStart(run));
            th.Start();
        }

        public void Stop()
        {
            m_Continue = false;
        }

        private void run()
        {
            while (m_Continue)
            {
                try
                {
                    if (m_Connection.State != System.Data.ConnectionState.Open)
                    {
                        m_Connection.Open();
                    }

                    string paramOper = "?";
                    if (m_Connection is MySql.Data.MySqlClient.MySqlConnection)
                    {
                        paramOper = "?date";
                    }

                    /*Detect eventtype*/

                    string scmd = "select eventForm from events where eventid = " + m_EventID;
                    IDbCommand cmd = m_Connection.CreateCommand();
                    cmd.CommandText = scmd;

                    string form = cmd.ExecuteScalar() as string;
                    bool isRelay = false;

                    if (form == "relay")
                        isRelay = true;



                    string baseCommand = "select results.modifyDate, results.totalTime, results.position, persons.familyname as lastname, persons.firstname as firstname, clubs.name as clubname, eventclasses.shortName, results.runnerStatus, results.entryid from results, entries, Persons, Clubs, raceclasses,eventclasses where raceclasses.eventClassID = eventClasses.eventClassID and results.raceClassID = raceclasses.raceclassid and raceClasses.eventRaceId = " + m_EventRaceId + " and eventclasses.eventid = " + m_EventID + " and results.entryid = entries.entryid and entries.competitorid = persons.personid and persons.clubid = clubs.clubid and results.runnerStatus != 'notActivated' and results.modifyDate > " + paramOper;
                    string splitbaseCommand = "select splittimes.modifyDate, splittimes.passedTime, Controls.ID, results.entryid, results.allocatedStartTime, persons.familyname as lastname, persons.firstname as firstname, clubs.name as clubname, eventclasses.shortName, splittimes.passedCount from splittimes, results, SplitTimeControls, Controls, eventClasses, raceClasses, Persons, Clubs, entries where splittimes.resultraceindividualnumber = results.resultid and SplitTimes.splitTimeControlID = SplitTimeControls.splitTimeControlID and SplitTimeControls.timingControl = Controls.controlid and Controls.eventRaceId = " + m_EventRaceId + " and raceclasses.eventClassID = eventClasses.eventClassID and results.raceClassID = raceclasses.raceclassid and raceClasses.eventRaceId = " + m_EventRaceId + " and eventclasses.eventid = " + m_EventID + " and results.entryid = entries.entryid and entries.competitorid = persons.personid and persons.clubid = clubs.clubid and splitTimes.modifyDate > " + paramOper;

                    if (isRelay)
                    {
                        baseCommand = "select results.modifyDate,results.totalTime, results.position, persons.familyname as lastname, persons.firstname as firstname, entries.teamName as clubname, eventclasses.shortName, raceclasses.relayleg, results.runnerStatus, results.relayPersonId as entryId, results.finishTime, results.allocatedStartTime from results, entries, Persons, Clubs, raceclasses,eventclasses where raceclasses.eventClassID = eventClasses.eventClassID and results.raceClassID = raceclasses.raceclassid and raceClasses.eventRaceId = " + m_EventRaceId + " and eventclasses.eventid = " + m_EventID + " and results.entryid = entries.entryid and results.relaypersonid = persons.personid and persons.clubid = clubs.clubid and results.runnerStatus != 'notActivated' and results.modifyDate > " + paramOper;
                        splitbaseCommand = "select splittimes.modifyDate, splittimes.passedTime, Controls.ID, results.relayPersonId as entryId, results.allocatedStartTime, persons.familyname as lastname, persons.firstname as firstname, entries.teamName as clubname, eventclasses.shortName,raceclasses.relayleg, splittimes.passedCount,results.allocatedStartTime  from splittimes, results, SplitTimeControls, Controls, eventClasses, raceClasses, Persons, Clubs, entries where splittimes.resultraceindividualnumber = results.resultid and SplitTimes.splitTimeControlID = SplitTimeControls.splitTimeControlID and SplitTimeControls.timingControl = Controls.controlid and Controls.eventRaceId = " + m_EventRaceId + " and raceclasses.eventClassID = eventClasses.eventClassID and results.raceClassID = raceclasses.raceclassid and raceClasses.eventRaceId = " + m_EventRaceId + " and eventclasses.eventid = " + m_EventID + " and results.entryid = entries.entryid and results.relaypersonid = persons.personid and persons.clubid = clubs.clubid and splitTimes.modifyDate > " + paramOper;
                    }
                    
                    cmd.CommandText = baseCommand; //new OleDbCommand(baseCommand, m_Connection);
                    IDbCommand cmdSplits = m_Connection.CreateCommand();// new OleDbCommand(splitbaseCommand, m_Connection);
                    cmdSplits.CommandText = splitbaseCommand;
                    IDbDataParameter param = cmd.CreateParameter();
                    param.ParameterName = "date";
                    if (m_Connection is MySql.Data.MySqlClient.MySqlConnection)
                    {
                        param.DbType = DbType.String;
                        param.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        param.DbType = DbType.DateTime;
                        param.Value = DateTime.Now;
                    }
                    

                    IDbDataParameter splitparam = cmdSplits.CreateParameter();
                    splitparam.ParameterName = "date";
                    if (m_Connection is MySql.Data.MySqlClient.MySqlConnection)
                    {
                        splitparam.DbType = DbType.String;
                        splitparam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        splitparam.DbType = DbType.DateTime;
                        splitparam.Value = DateTime.Now;
                    }
                    //OleDbParameter param = new OleDbParameter("@date", DateTime.Now);
                    //OleDbParameter splitparam = new OleDbParameter("@date", DateTime.Now);
                    //param.DbType = DbType.DateTime;
                    //splitparam.DbType = DbType.DateTime;

                    //param.OleDbType = OleDbType.DBTimeStamp;
                    DateTime lastDateTime = DateTime.Now.AddMonths(-120);
                    DateTime lastSplitDateTime = DateTime.Now.AddMonths(-120);
                    param.Value = lastDateTime;
                    splitparam.Value = lastSplitDateTime;

                    cmd.Parameters.Add(param);
                    cmdSplits.Parameters.Add(splitparam);

                    FireLogMsg("OLA Monitor thread started");
                    while (m_Continue)
                    {
                        IDataReader reader = null;
                        string lastRunner = "";
                        try
                        {
                            /*Kontrollera om nya klasser*/
                            /*Kontrollera om nya resultat*/
                            if (cmd is MySql.Data.MySqlClient.MySqlCommand)
                            {
                                (cmd.Parameters["date"] as IDbDataParameter).Value = lastDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                (cmdSplits.Parameters["date"] as IDbDataParameter).Value = lastSplitDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"); ;
                            }
                            else
                            {
                                (cmd.Parameters["date"] as IDbDataParameter).Value = lastDateTime;
                                (cmdSplits.Parameters["date"] as IDbDataParameter).Value = lastSplitDateTime;
                            }
                            

                            string command = cmd.CommandText;
                            cmd.Prepare();
                            reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                DateTime modDate = DateTime.MinValue;
                                int time = 0, position = 0, runnerID = 0;
                                string famName = "", fName = "", club = "", classN = "", status = "";
                                try
                                {
                                    //modDate = Convert.ToDateTime(reader[0]);
                                    string sModDate = Convert.ToString(reader[0]);
                                    if (sModDate.Contains("."))
                                    {
                                        modDate = DateTime.ParseExact(sModDate, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                                    }
                                    else
                                    {
                                        modDate = DateTime.ParseExact(sModDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                                    }

                                    lastDateTime = (modDate > lastDateTime ? modDate : lastDateTime);
                                    runnerID = Convert.ToInt32(reader["entryid"]);
                                    
                                    time = -9;
                                    if (!reader.IsDBNull(reader.GetOrdinal("totalTime")))
                                        time = Convert.ToInt32(reader["totalTime"]);

                                    famName = reader["lastname"] as string;
                                    fName = reader["firstname"] as string;
                                    lastRunner = (string.IsNullOrEmpty(fName) ? "" : (fName + " ")) + famName;
                                    
                                    club = reader["clubname"] as string; //.GetString(5);
                                    classN = reader["shortname"] as string; // reader.GetString(6);
                                    status = reader["runnerStatus"] as string; // reader.GetString(7);

                                    if (isRelay)
                                    {
                                        classN = classN + "-" + Convert.ToString(reader["relayLeg"]);
                                        if (reader["finishTime"] != DBNull.Value)
                                        {
                                            //DateTime ft = Convert.ToDateTime(reader["finishTime"]);
                                            //DateTime ast = Convert.ToDateTime(reader["allocatedStartTime"]);
                                            
                                            //time = (int)(((TimeSpan)(ft - ast)).TotalSeconds * 100);
                                        }
                                    }
                                    
                                }
                                catch (Exception ee)
                                {
                                    FireLogMsg(ee.Message);
                                }


                                /*
                                    time is seconds * 100
                                 * 
                                 * valid status is
                                    notStarted
                                    finishedTimeOk
                                    disqualified
                                    finished
                                    movedUp
                                    walkOver
                                    started
                                    passed
                                    notValid
                                    notActivated
                                 */
                                //EMMAClient.RunnerStatus rstatus = EMMAClient.RunnerStatus.Passed;
                                int rstatus = 0;
                                switch (status)
                                {
                                    case "started":
                                        rstatus = 9;
                                        break;
                                    case "notActivated":
                                        rstatus = 10;
                                        //rstatus = EMMAClient.RunnerStatus.NotStartedYet;
                                        break;
                                    case "notStarted":
                                        rstatus = 1;
                                        //rstatus = EMMAClient.RunnerStatus.NotStarted;
                                        break;
                                    case "disqualified":
                                        rstatus = 4;
                                        break;
                                    case "notValid":
                                        rstatus = 3;
                                        //rstatus = EMMAClient.RunnerStatus.MissingPunch;
                                        break;
                                    case "walkOver":
                                        rstatus = 11;
                                        //rstatus = EMMAClient.RunnerStatus.WalkOver;
                                        break;
                                    case "movedUp":
                                        rstatus = 12;
                                        break;
                                }
                                if (rstatus != 9 && rstatus != 10)
                                    FireOnResult(runnerID, 0, fName + " " + famName, club, classN, 0, time, rstatus, new List<ResultStruct>());

                            }
                            reader.Close();

                            reader = cmdSplits.ExecuteReader();
                            while (reader.Read())
                            {
                                string smod = Convert.ToString(reader[0]);
                                DateTime mod;
                                if (smod.Contains("."))
                                    mod = DateTime.ParseExact(smod, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                                else 
                                    mod = DateTime.ParseExact(smod, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                                lastSplitDateTime = (mod > lastSplitDateTime ? mod : lastSplitDateTime);
                                DateTime pTime = DateTime.ParseExact(Convert.ToString(reader[1]), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                                int sCont = reader.GetInt32(2);
                                int entryid = Convert.ToInt32(reader["entryid"]);
                                DateTime startTime = DateTime.ParseExact(Convert.ToString(reader[4]), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                                int passedCount = reader.GetInt32(reader.GetOrdinal("passedCount"));
                                TimeSpan rTid = pTime - startTime;
                                double time = rTid.TotalMilliseconds / 10;
                                List<ResultStruct> times = new List<ResultStruct>();
                                ResultStruct t = new ResultStruct();
                                t.ControlCode = sCont + 1000*passedCount;
                                t.ControlNo = 0;
                                t.Time = (int)time;
                                times.Add(t);

                                string sfamName = reader["lastname"] as string;
                                string sfName = reader["firstname"] as string;
                                string name = (string.IsNullOrEmpty(sfName) ? "" : (sfName + " ")) + sfamName;

                                string club = reader["clubname"] as string; //.GetString(5);
                                string classn = reader["shortname"] as string; // reader.GetString(6);

                                if (isRelay)
                                {
                                    classn = classn + "-" + Convert.ToString(reader["relayLeg"]);
                                }

                                FireOnResult(entryid, 0,name,club,classn, 0, -2, 0, times);
                            }
                            reader.Close();

                            System.Threading.Thread.Sleep(1000);
                        }
                        catch (Exception ee)
                        {
                            if (reader != null)
                                reader.Close();
                            FireLogMsg("OLA Parser: " + ee.Message + " {parsing: " + lastRunner);

                            switch (m_Connection.State)
                            {
                                case ConnectionState.Broken:
                                case ConnectionState.Closed:
                                    m_Connection.Close();
                                    m_Connection.Open();
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ee)
                {
                    FireLogMsg("OLA Parser: " +ee.Message);
                }
                finally
                {
                    if (m_Connection != null)
                    {
                        m_Connection.Close();
                    }
                    FireLogMsg("Disconnected");
                    FireLogMsg("OLA Monitor thread stopped");

                }
            }
        }
    }
}