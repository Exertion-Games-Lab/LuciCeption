from datetime import datetime

class PrintLogger:
    def __init__(self, filename="log.txt"):
        self.text = ""
        self.filename = filename

        # 清空檔案內容以開始新的 log
        with open(self.filename, 'w', encoding='utf-8') as f:
            f.write("")

    def print(self, *args, sep=' ', end='\n'):
        message = sep.join(str(arg) for arg in args)
        timestamp = datetime.now().strftime("[%Y-%m-%d %H:%M:%S] ")
        full_message = timestamp + message + end

        print(full_message, end='')        # 顯示在 terminal
        self.text += full_message          # 存進記憶體
        with open(self.filename, 'a', encoding='utf-8') as f:
            f.write(full_message)          # 寫進檔案

    def get_text(self):
        return self.text
