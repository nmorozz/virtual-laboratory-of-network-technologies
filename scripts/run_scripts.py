import os
import sys
import subprocess
import ctypes

def is_admin():
    """Проверяет, запущен ли скрипт с правами администратора."""
    try:
        return os.getuid() == 0  # Linux/macOS
    except AttributeError:
        return ctypes.windll.shell32.IsUserAnAdmin() != 0  # Windows

def run_as_admin():
    """Перезапускает скрипт с правами администратора."""
    if not is_admin():
        script = os.path.abspath(sys.argv[0])
        params = ' '.join(f'"{arg}"' for arg in sys.argv[1:])
        subprocess.run(["powershell", "Start-Process", "python", f"'-File {script} {params}'", "-Verb", "RunAs"], check=True)
        sys.exit()

def run_scripts(scripts):
    """Запускает все переданные скрипты."""
    for script in scripts:
        script_path = os.path.abspath(script)
        if os.path.exists(script_path):
            print(f"Запускаем {script}...")
            subprocess.run(["python", script_path], check=True)
        else:
            print(f"Ошибка: Файл {script} не найден.")

if __name__ == "__main__":
    run_as_admin()
    run_scripts(sys.argv[1:])
