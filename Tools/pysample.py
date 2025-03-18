
import sys
import string
import time

def get_Command():
    print('Meta>', end='')
    sys.stdout.flush()
    cmd= input() 
    return cmd
    
def do_Looping(): 
 
    while True:
        command = get_Command()
        if command.lower() == "exit":
            return
        elif len(command) > 0:
            process_Command(command)
          
def process_Command(command): 
    print("processing ", command)
    #time.sleep(1.5)
    print(command, "Done!")
     
def main():
     
    if len(sys.argv) < 2:
        print("parameter error!")
        sys.exit(255);

    args = sys.argv[1:]
    if args[0].lower() == "/loop":
        do_Looping()
        sys.exit(0)
    elif args[0].lower() == "/run" and len(args) >= 2:
        process_Command(args[1])
        sys.exit(0)
    else:
        print("unknown command ", args[0])
        sys.exit(255)

if __name__ == "__main__":
    main()