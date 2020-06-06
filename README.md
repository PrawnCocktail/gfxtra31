## gfxtra31 Scraper

**How to use**   
Double click the exe.    
If you want to scrape a particular category, then enter the url here, for instance, "https://www.gfxtra31.com/wordpress-templates/".    
Enter the first page number you want to scrape, if no number is entered it will scrape from page one.    
Enter the last page number you want to scrape, if no number is entered it will scrape all pages.    
You can use it as command line, such as "gfxtra31.exe url=https://www.gfxtra31.com/wordpress-templates/ first=120 last=400", this will scrap the wordpress category from pages 120 to 400.    
 
**What it does**  
- Scrapes the sitemap or category url for posts and gets the filenext urls.   
- Dumps the page title, page url and filenext url to a CSV file.   
- Supports multiple urls per page, seperated in the csv by a semicolon.    

**What it doesn't do**  
- Doesn't download any of the files.  
- Doesn't scrape passwords.   
- Doesn't check for duplicate entries.    

"Hey, your code sucks!" Yup, thats why I do this for fun and not for a living.  