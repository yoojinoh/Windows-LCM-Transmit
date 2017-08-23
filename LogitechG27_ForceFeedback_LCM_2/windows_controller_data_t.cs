/* LCM type definition class file
 * This file was automatically generated by lcm-gen
 * DO NOT MODIFY BY HAND!!!!
 */

using System;
using System.Collections.Generic;
using System.IO;
using LCM.LCM;
 
namespace wincontrollerlcm
{
    public sealed class windows_controller_data_t : LCM.LCM.LCMEncodable
    {
        public double mode;
        public double impedance_scale;
        public double balance_scale;
        public double enable;
        public double emergency_damp;
        public double[] p_des;
        public double[] v_des;
        public double[] rpy_des;
        public double[] omega_des;
        public double[] p_des_slew_min;
        public double[] p_des_slew_max;
        public double[] rpy_des_slew_max;
        public double[] v_des_slew_min;
        public double[] v_des_slew_max;
        public double[] omegab_des_slew_max;
        public double bonus_knee_torque;
 
        public windows_controller_data_t()
        {
            p_des = new double[3];
            v_des = new double[3];
            rpy_des = new double[3];
            omega_des = new double[3];
            p_des_slew_min = new double[3];
            p_des_slew_max = new double[3];
            rpy_des_slew_max = new double[3];
            v_des_slew_min = new double[3];
            v_des_slew_max = new double[3];
            omegab_des_slew_max = new double[3];
        }
 
        public static readonly ulong LCM_FINGERPRINT;
        public static readonly ulong LCM_FINGERPRINT_BASE = 0x8d8dae874eab677cL;
 
        static windows_controller_data_t()
        {
            LCM_FINGERPRINT = _hashRecursive(new List<String>());
        }
 
        public static ulong _hashRecursive(List<String> classes)
        {
            if (classes.Contains("wincontrollerlcm.windows_controller_data_t"))
                return 0L;
 
            classes.Add("wincontrollerlcm.windows_controller_data_t");
            ulong hash = LCM_FINGERPRINT_BASE
                ;
            classes.RemoveAt(classes.Count - 1);
            return (hash<<1) + ((hash>>63)&1);
        }
 
        public void Encode(LCMDataOutputStream outs)
        {
            outs.Write((long) LCM_FINGERPRINT);
            _encodeRecursive(outs);
        }
 
        public void _encodeRecursive(LCMDataOutputStream outs)
        {
            outs.Write(this.mode); 
 
            outs.Write(this.impedance_scale); 
 
            outs.Write(this.balance_scale); 
 
            outs.Write(this.enable); 
 
            outs.Write(this.emergency_damp); 
 
            for (int a = 0; a < 3; a++) {
                outs.Write(this.p_des[a]); 
            }
 
            for (int a = 0; a < 3; a++) {
                outs.Write(this.v_des[a]); 
            }
 
            for (int a = 0; a < 3; a++) {
                outs.Write(this.rpy_des[a]); 
            }
 
            for (int a = 0; a < 3; a++) {
                outs.Write(this.omega_des[a]); 
            }
 
            for (int a = 0; a < 3; a++) {
                outs.Write(this.p_des_slew_min[a]); 
            }
 
            for (int a = 0; a < 3; a++) {
                outs.Write(this.p_des_slew_max[a]); 
            }
 
            for (int a = 0; a < 3; a++) {
                outs.Write(this.rpy_des_slew_max[a]); 
            }
 
            for (int a = 0; a < 3; a++) {
                outs.Write(this.v_des_slew_min[a]); 
            }
 
            for (int a = 0; a < 3; a++) {
                outs.Write(this.v_des_slew_max[a]); 
            }
 
            for (int a = 0; a < 3; a++) {
                outs.Write(this.omegab_des_slew_max[a]); 
            }
 
            outs.Write(this.bonus_knee_torque); 
 
        }
 
        public windows_controller_data_t(byte[] data) : this(new LCMDataInputStream(data))
        {
        }
 
        public windows_controller_data_t(LCMDataInputStream ins)
        {
            if ((ulong) ins.ReadInt64() != LCM_FINGERPRINT)
                throw new System.IO.IOException("LCM Decode error: bad fingerprint");
 
            _decodeRecursive(ins);
        }
 
        public static wincontrollerlcm.windows_controller_data_t _decodeRecursiveFactory(LCMDataInputStream ins)
        {
            wincontrollerlcm.windows_controller_data_t o = new wincontrollerlcm.windows_controller_data_t();
            o._decodeRecursive(ins);
            return o;
        }
 
        public void _decodeRecursive(LCMDataInputStream ins)
        {
            this.mode = ins.ReadDouble();
 
            this.impedance_scale = ins.ReadDouble();
 
            this.balance_scale = ins.ReadDouble();
 
            this.enable = ins.ReadDouble();
 
            this.emergency_damp = ins.ReadDouble();
 
            this.p_des = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                this.p_des[a] = ins.ReadDouble();
            }
 
            this.v_des = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                this.v_des[a] = ins.ReadDouble();
            }
 
            this.rpy_des = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                this.rpy_des[a] = ins.ReadDouble();
            }
 
            this.omega_des = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                this.omega_des[a] = ins.ReadDouble();
            }
 
            this.p_des_slew_min = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                this.p_des_slew_min[a] = ins.ReadDouble();
            }
 
            this.p_des_slew_max = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                this.p_des_slew_max[a] = ins.ReadDouble();
            }
 
            this.rpy_des_slew_max = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                this.rpy_des_slew_max[a] = ins.ReadDouble();
            }
 
            this.v_des_slew_min = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                this.v_des_slew_min[a] = ins.ReadDouble();
            }
 
            this.v_des_slew_max = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                this.v_des_slew_max[a] = ins.ReadDouble();
            }
 
            this.omegab_des_slew_max = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                this.omegab_des_slew_max[a] = ins.ReadDouble();
            }
 
            this.bonus_knee_torque = ins.ReadDouble();
 
        }
 
        public wincontrollerlcm.windows_controller_data_t Copy()
        {
            wincontrollerlcm.windows_controller_data_t outobj = new wincontrollerlcm.windows_controller_data_t();
            outobj.mode = this.mode;
 
            outobj.impedance_scale = this.impedance_scale;
 
            outobj.balance_scale = this.balance_scale;
 
            outobj.enable = this.enable;
 
            outobj.emergency_damp = this.emergency_damp;
 
            outobj.p_des = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                outobj.p_des[a] = this.p_des[a];
            }
 
            outobj.v_des = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                outobj.v_des[a] = this.v_des[a];
            }
 
            outobj.rpy_des = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                outobj.rpy_des[a] = this.rpy_des[a];
            }
 
            outobj.omega_des = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                outobj.omega_des[a] = this.omega_des[a];
            }
 
            outobj.p_des_slew_min = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                outobj.p_des_slew_min[a] = this.p_des_slew_min[a];
            }
 
            outobj.p_des_slew_max = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                outobj.p_des_slew_max[a] = this.p_des_slew_max[a];
            }
 
            outobj.rpy_des_slew_max = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                outobj.rpy_des_slew_max[a] = this.rpy_des_slew_max[a];
            }
 
            outobj.v_des_slew_min = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                outobj.v_des_slew_min[a] = this.v_des_slew_min[a];
            }
 
            outobj.v_des_slew_max = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                outobj.v_des_slew_max[a] = this.v_des_slew_max[a];
            }
 
            outobj.omegab_des_slew_max = new double[(int) 3];
            for (int a = 0; a < 3; a++) {
                outobj.omegab_des_slew_max[a] = this.omegab_des_slew_max[a];
            }
 
            outobj.bonus_knee_torque = this.bonus_knee_torque;
 
            return outobj;
        }
    }
}
