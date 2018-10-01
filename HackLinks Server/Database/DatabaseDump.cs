using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Database
{
    class DatabaseDump
    {
        private static List<string> commands = new List<string>
        {
            "/*!40101 SET @saved_cs_client     = @@character_set_client */",
            "/*!40101 SET character_set_client = utf8 */",
            "DROP TABLE IF EXISTS `accounts`",
            "CREATE TABLE `accounts` ("+
            " `id` int(11) NOT NULL AUTO_INCREMENT,"+
            " `username` varchar(64) NOT NULL,"+
            " `pass` char(64) DEFAULT NULL," +
            " `mailaddress` varchar(64) DEFAULT NULL,"+
            " `netmap` TEXT NOT NULL," +
            " `homeComputer` int(11) DEFAULT NULL,"+
            " `permissions` TEXT NOT NULL,"+
            " `banned` INT DEFAULT NULL,"+
            " `permBanned` BOOLEAN NOT NULL DEFAULT FALSE,"+
            " PRIMARY KEY (`id`),"+
            " UNIQUE KEY `username` (`username`),"+
            " UNIQUE KEY `mailaddress` (`mailaddress`)"+
            ") ENGINE = InnoDB AUTO_INCREMENT = 2 DEFAULT CHARSET = latin1",
            //
            // Dumping data for table `accounts`
            //
            "LOCK TABLES `accounts` WRITE",
            "/*!40000 ALTER TABLE `accounts` DISABLE KEYS */",
            "INSERT INTO `accounts` VALUES (1,'test','e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855','test@hnmp.net','',1,'admin',false,false)",
            "/*!40000 ALTER TABLE `accounts` ENABLE KEYS */",
            "UNLOCK TABLES",
            "/*!40101 SET character_set_client = @saved_cs_client */",
            "/*!40101 SET @saved_cs_client     = @@character_set_client */",
            "/*!40101 SET character_set_client = utf8 */",

            //
            // Dumping data for table `computers`
            //
            "DROP TABLE IF EXISTS `computers`",
            "CREATE TABLE `computers` (" +
            " `id` int(10) unsigned NOT NULL AUTO_INCREMENT," +
            " `ip` varchar(15) NOT NULL," +
            " `owner` varchar(64) NOT NULL," +
            " `type` int(11) NOT NULL," +
            " PRIMARY KEY (`id`)," +
            " UNIQUE KEY `ip` (`ip`)" +
            ") ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=latin1",
            "LOCK TABLES `computers` WRITE",
            "/*!40000 ALTER TABLE `computers` DISABLE KEYS */",
            "INSERT INTO `computers` VALUES (1,'8.8.8.8',1,4)",
            "/*!40000 ALTER TABLE `computers` ENABLE KEYS */",
            "UNLOCK TABLES",
            "/*!40101 SET character_set_client = @saved_cs_client */",
            "/*!40101 SET @saved_cs_client     = @@character_set_client */",
            "/*!40101 SET character_set_client = utf8 */",

            //
            // Dumping data for table `files`
            //
            "DROP TABLE IF EXISTS `files`",
            "CREATE TABLE files ("+
            " computer_id int NOT NULL,"+
            " filesystem_id int  NOT NULL,"+
            " inode int NOT NULL,"+
            " link_count int DEFAULT 0 NOT NULL,"+
            " mode int NOT NULL,"+
            " group_id int  NOT NULL,"+
            " user_id int  NOT NULL,"+
            " inode_change_time TIMESTAMP DEFAULT NOW(),"+
            " last_modified_time TIMESTAMP DEFAULT NOW(),"+
            " last_accessed_time TIMESTAMP DEFAULT NOW(),"+
            " content BLOB,"+
            " CONSTRAINT PK_file PRIMARY KEY (computer_id, filesystem_id, inode)"+
            ")",
            "INSERT INTO files " +
            "(computer_id, filesystem_id, inode, mode, group_id, user_id, content, link_count) "+
            " VALUES "
            +$"(1, 0, 1, {DBUtil.GenerateMode(FileType.Directory, Permission.A_All)}, 1, 0, UNHEX('{FileUtil.BuildFileListing(new FileUtil.DirRecord(0, 2, "etc"), new FileUtil.DirRecord(0, 5, "bin"))}'), 1), " // We give root one "magic" link count. It's to stop it being removed.
            +$"(1, 0, 2, {DBUtil.GenerateMode(FileType.Directory, Permission.A_All)}, 1, 0, UNHEX('{FileUtil.BuildFileListing(new FileUtil.DirRecord(0, 3, "passwd"), new FileUtil.DirRecord(0, 4, "group"))}'), 1), "
            +$"(1, 0, 3, {DBUtil.GenerateMode(FileType.Regular, Permission.A_All)}, 1, 0, '" +
            "root:x:0:0:root:/root:/bin/hash\r\n" +
            "admin:x:1:1:root:/root:/bin/hash\r\n" +
            "user:x:2:2:root:/root:/bin/hash\r\n" +
            "guest:x:3:3:root:/root:/bin/hash\r\n" +
            "', 1), "
            +$"(1, 0, 4, {DBUtil.GenerateMode(FileType.Regular, Permission.A_All)}, 1, 0, '" +
            "root:x:0:\r\n" +
            "admin:x:1:root,admin\r\n" +
            "user:x:2:root,admin,user\r\n" +
            "guest:x:3:root,admin,user,guest\r\n" +
            "', 1), "
            +$"(1, 0, 5, {DBUtil.GenerateMode(FileType.Directory, Permission.A_All)}, 1, 0, UNHEX('{FileUtil.BuildFileListing(new FileUtil.DirRecord(0, 6, "hackybox"), new FileUtil.DirRecord(0, 6, "login"), new FileUtil.DirRecord(0, 6, "ls"), new FileUtil.DirRecord(0, 6, "rm"), new FileUtil.DirRecord(0, 6, "touch"), new FileUtil.DirRecord(0, 6, "netmap"), new FileUtil.DirRecord(0, 6, "mv"), new FileUtil.DirRecord(0, 6, "cp"))}'), 1), "
            +$"(1, 0, 6, {DBUtil.GenerateMode(FileType.Regular, Permission.A_All)}, 1, 0, 'hackybox', 3), " // Important. Increment count for each new copy
            +$"(1, 0, 99, {DBUtil.GenerateMode(FileType.Regular, Permission.A_All)}, 1, 0, NULL, 1) "
            ,

            "DROP TABLE IF EXISTS `binaries`",
            "CREATE TABLE `binaries` (" +
            " `id` int(11) PRIMARY KEY NOT NULL AUTO_INCREMENT," +
            " `checksum` int NOT NULL," +
            " `type` char(64) NOT NULL" +
            ") ENGINE = InnoDB AUTO_INCREMENT = 1 DEFAULT CHARSET = latin1",
            //
            // Dumping data for table `binaries`
            //
            $"INSERT INTO `binaries` VALUES "+
            $"(0,{HashUtil.CalcMurmur("hackybox")},'Hackybox'),"+
            $"(0,{HashUtil.CalcMurmur("serveradmin")},'ServerAdmin'),"+
            $"(0,{HashUtil.CalcMurmur("computeradmin")},'ComputerAdmin')",
        };

        public static List<string> Commands => commands;
    }
}
