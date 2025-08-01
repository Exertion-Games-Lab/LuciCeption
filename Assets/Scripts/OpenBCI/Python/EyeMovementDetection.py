from LuciDetector import SyntheticDetector
from LuciServer import DetectorServer, DetectorStateMachine

def runSystem():
  board = SyntheticDetector()
  stateMachine = DetectorStateMachine(board)
  server = DetectorServer(stateMachine)
  stateMachine.setServer(server)

if __name__ == "__main__":
  runSystem()
