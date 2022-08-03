using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downloader
{
    public class Categories
    {
        public string LastUpdate { get; set; }
        public List<Category> Elements { get; set; }
        public Categories()
        {
            Elements = new List<Category>();
        }

    }
    public class Models
    {
        public string LastUpdate { get; set; }
        public List<Model> Elements { get; set; }
        public Models()
        {
            Elements = new List<Model>();
        }
    }
    public class Users
    {
        public List<User> Elements { get; set; }
        public Users()
        {
            Elements = new List<User>();
        }
    }
    public interface ITitle
    {
        string Title { get; set; }
    }
    public interface ILocalInfo
    {
        bool IsLocal { get; set; }
        string LocalPath { get; set; }
    }
    public class VideoDirectGroup
    {
        public string Link { get; set; }
        public List<PTrexVideoDirect> Videos { get; set; }
    }
    public class PTrexVideoDirect : ITitle
    {
        public string Title { get; set; }
        public string Quality { get; set; }
        public string Size { get; set; }
        public string UserLink { get; set; }
        public string Link { get; set; }

    }
    public class InfoClass : ITitle
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public int NumberOfVideos { get; set; }
        public int Rating { get; set; }

    }
    public class Category : InfoClass, ILocalInfo
    {
        public bool IsLocal { get; set; }
        public string LocalPath { get; set; }
    }
    public class Model : InfoClass, ILocalInfo
    {
        public bool IsLocal { get; set; }
        public string LocalPath { get; set; }
    }
    public class User : ILocalInfo
    {
        public string Number { get; set; }
        public string Link { get; set; }
        public int TotalNumberOfVideos { get; set; }
        public bool IsLocal { get; set; }
        public string LocalPath { get; set; }

    }
}