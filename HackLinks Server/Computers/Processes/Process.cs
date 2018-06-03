﻿using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Processes
{
    public abstract class Process
    {
        public delegate void Printer(string text);

        public enum State
        {
            New, // New and not running
            Running, // Running
            Stopped, // Stopped and not running (no longer receiving Update ticks)
            Dead // Dead and waiting for it's exit code to be retrieived so that it may be cleared from the process table
        }
        /// <summary>
        /// Control signals sent to a process by the host <see cref="Node"/>.
        /// </summary>
        public enum ProcessSignal
        {
            /// <summary>
            /// Sent to kill a process. The process is killed immediately and is unable to handle the signal.
            /// </summary>
            SIGKILL,
            /// <summary>
            ///  Sent when a hangup is detected. e.g. terminal window closed.
            /// </summary>
            SIGHUP,
            /// <summary>
            /// Sent to terminate a process, politely. This usually kills the process but can be ignored/handled by extending <see cref="HandleSignal(ProcessSignal)"/>.
            /// </summary>
            SIGTERM,
            /// <summary>
            /// Sent when an interrupt is detected (e.g. ^C is pressed). This is usually generated by the tty device. This usually kills the process but can be ignored/handled by extending <see cref="HandleSignal(ProcessSignal)"/>.
            /// </summary>
            SIGINT,
            /// <summary>
            /// Sent to a process that attempts to write to a pipe with no readers.
            /// </summary>
            SIGPIPE,
            /// <summary>
            /// Sent when a child process changes state. Accompanying data includes the PID, UID and exit status. Used for job control. No action by default. Handled entirely in <see cref="HandleSignal(ProcessSignal)"/>.
            /// </summary>
            SIGCHLD,
            /// <summary>
            /// Unconditionally suspend a process. Used internally. You'll usually want to deal with <see cref="SIGTSTP"/> instead. Cannot be handled at all by <see cref="HandleSignal(ProcessSignal)"/>.
            /// </summary>
            SIGSTOP,
            /// <summary>
            /// Wakes up a stopped process. Cannot be ignored but additional actions can be performed after the process is running by extending <see cref="HandleSignal(ProcessSignal)"/>.
            /// </summary>
            SIGCONT,
            /// <summary>
            /// This usually suspends a process (e.g. ^Z pressed). but can be ignored by extending <see cref="HandleSignal(ProcessSignal)"/>.
            /// </summary>
            SIGTSTP,
            /// <summary>
            /// Sent to a background process Group/Job if a member attempts to read from TTY. This usually suspends the process but can be ignored/handled by extending <see cref="HandleSignal(ProcessSignal)"/>.
            /// </summary>
            SIGTTIN,
            /// <summary>
            ///  Sent to a background process Group/Job if a member attempts to write to a TTY. This usually suspends the process but can be ignored/handled by extending <see cref="HandleSignal(ProcessSignal)"/>.
            /// </summary>
            SIGTTOU,
        }

        public readonly int ProcessId;
        public Node computer;

        public File ActiveDirectory { get; set; }
        
        public Credentials Credentials { get; }

        private State currentState;
        public State CurrentState { get => currentState; private set { computer.NotifyProcessStateChange(ProcessId, value); currentState = value; } }

        protected byte exitCode = 0;
        public byte ExitCode => exitCode;

        private readonly Printer printer;

        public Printer Print => printer;

        public Process(int pid, Printer printer, Node computer, Credentials credentials)
        {
            ProcessId = pid;
            this.printer = printer ?? delegate { };
            this.computer = computer;
            ActiveDirectory = computer.fileSystem.rootFile;
            Credentials = credentials;

            CurrentState = State.New;

            computer.RegisterProcess(this);
        }

        /// <summary>
        /// Overridden to react to child death in some way, e.g. retrieve exit code.
        /// Dead children do not receive updates and are removed from the process table following the return of this function.
        /// </summary>
        /// <param name="process"></param>
        public virtual void NotifyDeadChild(Process process)
        {
            computer.Kernel.ReattachParent(this, process);
        }

        /// <summary>
        /// Overridden to provide inital application startup
        /// </summary>
        /// <param name="command">The full command used to launch the application</param>
        public virtual void Run(string command)
        {
            CurrentState = State.Running;
        }

        /// <summary>
        /// Overridden to provide regular periodic updates to long running processes.
        /// It is the responsibility of the implementor to ensure this returns in a timely manner.
        /// </summary>
        public virtual void Update()
        {

        }

        /// <summary>
        /// Overridden to provide input to a running command (effectively STDIN for the process)
        /// </summary>
        /// <param name="inputData"></param>
        public virtual void WriteInput(string inputData)
        {

        }

        public void Signal(ProcessSignal signal)
        {
            switch (signal)
            {
                case ProcessSignal.SIGKILL:
                    CurrentState = State.Dead;
                    break;
                case ProcessSignal.SIGHUP:
                    if (!HandleSignal(signal))
                    {
                        CurrentState = State.Dead;
                    }
                    break;
                case ProcessSignal.SIGTERM:
                    if (!HandleSignal(signal))
                    {
                        CurrentState = State.Dead;
                    }
                    break;
                case ProcessSignal.SIGINT:
                    if (!HandleSignal(signal))
                    {
                        CurrentState = State.Dead;
                    }
                    break;
                case ProcessSignal.SIGPIPE:
                    if (!HandleSignal(signal))
                    {
                        CurrentState = State.Dead;
                    }
                    break;
                case ProcessSignal.SIGCHLD:
                    HandleSignal(signal);
                    break;
                case ProcessSignal.SIGSTOP:
                    CurrentState = State.Stopped;
                    break;
                case ProcessSignal.SIGCONT:
                    if(currentState == State.Stopped)
                    {
                        currentState = State.Running;
                        HandleSignal(signal);
                    }
                    break;
                case ProcessSignal.SIGTSTP:
                    if (!HandleSignal(signal))
                    {
                        CurrentState = State.Stopped;
                    }
                    break;
                case ProcessSignal.SIGTTIN:
                    if (!HandleSignal(signal))
                    {
                        CurrentState = State.Stopped;
                    }
                    break;
                case ProcessSignal.SIGTTOU:
                    if (!HandleSignal(signal))
                    {
                        CurrentState = State.Stopped;
                    }
                    break;
            }
        }

        protected virtual bool HandleSignal(ProcessSignal signal)
        {
            return false;
        }
    }
}
